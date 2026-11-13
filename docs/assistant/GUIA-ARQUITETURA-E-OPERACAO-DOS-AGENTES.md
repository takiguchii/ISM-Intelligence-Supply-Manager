# Guia de Arquitetura e Operação dos Agentes do ISM

**Status:** diagnóstico inicial e especificação-base  
**Data:** 2026-11-12  
**Escopo:** agentes operacionais, ISM Assist, chat contextual, motores de IA, ações administrativas, segurança, testes e evolução para Python.

## 1. Objetivo

Este documento define como os agentes do ISM devem ser organizados, quais dados podem consultar, quais métricas devem produzir, quais ações podem propor ou executar e como o chat geral deve consultar essas capacidades sem quebrar o isolamento por restaurante.

Regra central:

> O banco e as regras determinísticas do ISM são a fonte de verdade. Modelos de linguagem explicam, classificam e conversam; não calculam valores críticos por conta própria, não executam SQL e não alteram dados sem uma ferramenta autorizada, validação e auditoria.

## 2. Diagnóstico do estado atual

### Já implementado

- `StockAgent`, `SupplierAgent` e `PricingCmvService` possuem contratos próprios e são executados pelo `AgentsBackgroundScheduler` a cada cinco minutos.
- Os agentes geram `SystemAlert` com `RestaurantId`, entidade de referência, severidade, mensagem, payload e deduplicação.
- O estoque calcula consumo médio de 30 dias, risco de ruptura em 48 horas e estoque abaixo do mínimo.
- Fornecedores analisam histórico de preços em 60 dias, aumento de preço e alternativas entre fornecedores.
- Precificação recalcula custo/CMV dos pratos e sugere preço para atingir a margem configurada.
- O `AssistantService` cria propostas, permite aprovação/rejeição/revisão e registra auditoria. A aprovação de preço valida concorrência e incrementa `PriceRevision`.
- O chat possui threads, histórico persistido, contexto de alerta e fallback determinístico quando o Ollama está indisponível.
- Há isolamento por restaurante nas APIs do Assistant e política `RestaurantManagerOrAbove`.
- Há testes unitários e de integração cobrindo alertas, aprovação, conflito de preço, tenant isolation, contexto e fallback.

### Lacunas e riscos encontrados

- O catálogo de agentes ainda cobre somente estoque, precificação e fornecedores. Não há agentes explícitos para financeiro, pedidos, cadastro, negociação, especificação, compras ou um orquestrador geral.
- O chat geral consulta sobretudo alertas recentes; ainda não possui ferramentas tipadas para consultar todo o domínio autorizado (financeiro, pedidos, cadastro, estoque, fornecedores e cardápio).
- Existe um provider concreto para Ollama. A configuração de endpoint/modelo por restaurante existe, mas ainda não representa uma abstração completa de múltiplos motores locais, web, API ou Python.
- O `StockAgent` persiste `MovingAverageConsumption` e o agente de precificação persiste `Dish.Cost`. Essas escritas devem ser declaradas como cálculos derivados autorizados e auditáveis, ou separadas em um estágio de materialização.
- A proposta de ação operacional é genérica (`ReviewAlert`); não existe ainda um fluxo completo para sugerir, aprovar e criar pedido de compra, alterar estoque, registrar financeiro ou negociar fornecedor.
- O chat não deve receber SQL bruto nem ganhar acesso direto ao `DbContext`. A expansão deve ser feita por ferramentas de consulta com DTOs mínimos e escopo tenant-scoped.
- O scheduler captura exceções por restaurante, mas não há ainda uma execução persistida com duração, falha, versão do agente, quantidade de dados consultados e correlação para observabilidade completa.

## 3. Catálogo de agentes e superfícies

| Agente | Superfícies | Dados permitidos | Saídas principais | Escritas permitidas na primeira fase |
|---|---|---|---|---|
| Estoque | Dashboard, estoque, pedidos, chat contextual | Produtos, saldos, mínimos, consumo, movimentações, pedidos abertos | risco de ruptura, reposição sugerida, consumo médio, alertas | materializar métricas derivadas; não movimentar estoque |
| Precificação/CMV | Dashboard, cardápio, financeiro, chat | Pratos, ingredientes, custos, preços, margem alvo, histórico de preço | CMV, margem, preço sugerido, alerta de defasagem | proposta de alteração de preço com aprovação |
| Fornecedores | Dashboard, fornecedores, compras, chat | Fornecedores, histórico de preço, produto, prazo e qualidade quando disponíveis | aumento, alternativa, cobertura, ranking e alerta | nenhuma mutação automática; proposta de compra/negociação |
| Financeiro | Financeiro, dashboard, chat | lançamentos, categorias, receitas, despesas, fluxo de caixa | desvios, vencimentos, tendências e conciliação pendente | somente propostas; nunca lançar pagamento automaticamente |
| Pedidos/Compras | pedidos, estoque, fornecedores, chat | pedidos, itens, status, estoque, preços e prazo | necessidade de compra, consolidação e acompanhamento | criar rascunho de pedido após aprovação |
| Cadastro/Especificação | cadastro, cardápio, estoque, fornecedores | produtos, unidades, categorias, pratos, ingredientes e campos obrigatórios | inconsistências, duplicidades e completude | proposta de correção; escrita somente com confirmação |
| Negociação | fornecedores, pedidos, chat | preço histórico, volume, alternativas e condições cadastradas | argumento, comparação e roteiro de negociação | registrar proposta/atividade; não comprometer fornecedor automaticamente |
| Chat geral/Orquestrador | todas as áreas administrativas | somente resultados das ferramentas autorizadas | resposta, síntese, encaminhamento ao especialista e contexto | nunca altera diretamente; encaminha ação para workflow aprovado |

As superfícies são pontos de entrada da experiência. Elas não concedem permissões extras ao agente. A autorização é definida pelo contrato do agente, pelo papel do usuário e pelo `RestaurantId`.

## 4. Contrato mínimo de cada agente

Cada agente deve possuir:

1. Identificador estável (`stock`, `pricing`, `suppliers`, `finance`, `orders`, `catalog`, `negotiation`, `general`).
2. Declaração de domínio, entidades consultáveis, métricas, ferramentas de leitura e ferramentas de escrita.
3. Política explícita de acesso: restaurante, papel mínimo, limites de paginação, período máximo e campos retornados.
4. Executor determinístico versionado, independente do LLM.
5. Resultado estruturado com `runId`, horário, versão, entidades avaliadas, métricas, alertas, erros e ações propostas.
6. Auditoria para toda proposta e toda escrita efetivada.
7. Testes de domínio, tenant isolation, autorização, concorrência, idempotência e falha de provider.

Interface conceitual recomendada:

```csharp
public interface IAgent
{
    string Key { get; }
    AgentCapabilityDescriptor Capabilities { get; }
    Task<AgentRunResult> RunAsync(AgentRunContext context, CancellationToken ct);
}
```

O contexto deve receber o restaurante já validado pelo backend. Nenhum agente deve aceitar `RestaurantId` vindo apenas do prompt do usuário.

## 5. Separação entre consulta, proposta e execução

Cada ação deve seguir este fluxo:

```text
Dados do tenant
  -> regra determinística do agente
  -> alerta ou recomendação estruturada
  -> proposta de ação com snapshot e evidências
  -> aprovação conforme papel e risco
  -> executor transacional
  -> auditoria, evento e atualização do alerta
```

Classificação de risco:

- **Leitura:** consultas e métricas; sem mutação.
- **Derivada:** recalcular/materializar uma métrica idempotente, como consumo médio ou custo calculado; deve ser auditável e não representar uma decisão comercial.
- **Proposta:** sugestão de preço, compra, negociação ou correção; não altera o negócio.
- **Mutação controlada:** alteração de preço, criação de pedido rascunho ou cadastro; exige aprovação, verificação de versão e auditoria.
- **Alto risco:** pagamentos, exclusões, alteração em massa, comunicação externa ou comprometimento com fornecedor; bloqueado para automação inicial e exige fluxo administrativo específico.

O LLM não deve chamar repositórios, gerar SQL, escolher tenant, ignorar aprovação ou transformar uma pergunta em mutação implícita.

## 6. Chat geral e chat contextual

### Chat contextual

Aberto a partir de um alerta ou entidade. Deve persistir `OriginAlertId`, entidade, snapshot factual, thread, usuário e restaurante. Perguntas sobre “por que”, “qual valor” e “qual risco” devem ser respondidas preferencialmente de forma determinística.

### Chat geral

É diferente dos especialistas: atua como interface de consulta e roteador. Ele deve:

- identificar a intenção e o domínio;
- chamar uma ou mais ferramentas de leitura autorizadas;
- combinar resultados sem inventar números;
- indicar a fonte e o horário dos dados;
- encaminhar ações para propostas, nunca executá-las no texto;
- preservar histórico, feedback e contexto da thread;
- informar quando os dados estão ausentes, atrasados ou fora do escopo.

Ferramentas iniciais recomendadas: `GetStockSummary`, `GetStockMovements`, `GetPriceAndMargin`, `GetSupplierPriceComparison`, `GetFinanceSummary`, `GetOpenOrders`, `GetEntityCompleteness` e `GetAlertContext`. Cada ferramenta retorna DTO validado, limite de registros e `asOfUtc`.

## 7. Motores de IA e multimodalidade

Criar uma abstração de provider sem acoplar o domínio ao Ollama:

```text
IAgentModelProvider
  ├── OllamaProvider       (local)
  ├── OpenAiCompatibleProvider (API)
  ├── WebProvider           (busca externa controlada)
  └── PythonAgentProvider   (serviço Python via HTTP/gRPC)
```

O roteador deve selecionar o provider por capacidade, configuração do restaurante, disponibilidade, custo e política de dados. O fallback deve ser explícito e observável. Dados privados do restaurante não podem ser enviados para web/API sem política, consentimento/configuração e redaction.

Multimodalidade deve ser uma capacidade declarada (`text`, `image`, `audio`, `web`) e não uma dependência obrigatória. O caminho determinístico dos agentes continua funcional se qualquer modelo estiver indisponível.

Para Python, recomenda-se um serviço separado com contrato versionado, autenticação serviço-a-serviço, timeout, limite de payload, correlação (`runId`) e retorno estruturado. O Python não deve acessar diretamente o banco principal na primeira fase; deve receber dados mínimos por ferramentas/DTOs do backend.

## 8. Administração

O administrador do restaurante poderá configurar, dentro do próprio tenant:

- agentes habilitados e periodicidade;
- provider, modelo, timeout e limites;
- modo somente leitura ou propostas habilitadas;
- tipos de alerta e canais de notificação;
- aprovação necessária por tipo de ação;
- permissões do chat e retenção de histórico;
- prompts/políticas versionados, sem permitir remover regras de segurança.

O SuperAdmin de plataforma poderá administrar catálogo global, capacidades, versões e observabilidade, mas não deve receber escrita implícita nos dados de um restaurante. Segredos devem permanecer no backend/secret store; nunca retornar chave completa ao frontend ou ao LLM.

## 9. Plano de implementação incremental

### Fase 1 — Fundação e contratos

- Criar `IAgent`, catálogo de capacidades, `AgentRun`, status e auditoria de execução.
- Formalizar políticas de domínio dos três agentes existentes.
- Separar consultas, materializações derivadas e propostas.
- Corrigir qualquer captura vazia de exceção e registrar falhas reais.

### Fase 2 — Segurança e ferramentas do chat

- Criar ferramentas tenant-scoped para estoque, precificação, fornecedores, financeiro, pedidos e cadastro.
- Fazer o chat geral usar somente ferramentas tipadas.
- Adicionar limites, fontes, data de coleta e redaction.

### Fase 3 — Ações controladas

- Expandir propostas para compra, negociação, cadastro e financeiro.
- Implementar aprovação por risco, idempotência, concorrência e auditoria.
- Criar pedido apenas como rascunho aprovado na primeira entrega.

### Fase 4 — Motores e Python

- Extrair interface de provider e adicionar health check, timeout, retry limitado e métricas.
- Integrar Python via serviço externo versionado.
- Adicionar seleção por capacidade e fallback determinístico.

### Fase 5 — Especialistas adicionais

- Implementar Financeiro, Pedidos/Compras, Cadastro/Especificação e Negociação somente depois que seus contratos e dados estiverem cobertos por testes.

## 10. Estratégia de testes pesados

### Testes obrigatórios por agente

- Dados reais do domínio geram métricas esperadas.
- Restaurante A nunca lê, alerta ou altera dados do restaurante B.
- Chef/garçom não acessam ações administrativas.
- Execução repetida não duplica alertas, propostas ou materializações.
- Falha de uma dependência não interrompe os demais restaurantes.
- Timeout/offline do provider não derruba alertas determinísticos.
- Dados nulos, unidades incompatíveis e valores extremos não produzem recomendação falsa.

### Testes de ação

- Aprovação altera somente a entidade prevista.
- Rejeição não altera entidade de negócio.
- Concorrência retorna conflito e não sobrescreve mudança posterior.
- Requisição repetida é idempotente.
- Toda mutação gera auditoria com usuário, agente, snapshot, evidências e horário.

### Testes do chat

- Chat contextual restaura thread e fatos após reload.
- Chat geral consulta a ferramenta correta para cada domínio.
- Pergunta sem dados retorna ausência explícita, sem números inventados.
- Prompt injection em nome de produto/fornecedor não altera política.
- O LLM não recebe SQL, segredo, dados de outro tenant ou instrução executável.
- 30–50 aberturas/fechamentos não acumulam listeners, timers ou threads indevidas.

### Testes operacionais

- Carga com vários restaurantes e grandes volumes de produtos/histórico.
- Execuções concorrentes do scheduler.
- MySQL indisponível, provider indisponível e resposta malformada.
- Latência, quantidade de consultas e memória por execução.
- Migração e rollback operacional em banco de demonstração antes de produção.

## 11. Critérios de aceite da arquitetura

Considera-se a fundação pronta quando:

1. Cada agente possui domínio, capacidades e limites registrados.
2. Toda consulta e toda ação são tenant-scoped e autorizadas.
3. Alertas e métricas determinísticas funcionam sem LLM.
4. O chat geral consulta ferramentas tipadas e mostra origem/data dos fatos.
5. Nenhuma ação mutável ocorre diretamente pelo texto do modelo.
6. Providers podem ser trocados sem alterar os agentes de negócio.
7. Python pode ser conectado por contrato externo sem acesso direto ao banco.
8. Falhas, execuções, propostas e mutações são auditáveis.
9. A suíte cobre isolamento, autorização, idempotência, concorrência e indisponibilidade.
10. O build e os testes do backend/frontend passam sem mascarar exceções.

## 12. Referências do código atual

- Scheduler: `backend/src/ISM.API/HostedServices/AgentsBackgroundScheduler.cs`
- Estoque: `backend/src/ISM.Application/Services/Stock/StockAgent.cs`
- Fornecedores: `backend/src/ISM.Application/Services/Suppliers/SupplierAgent.cs`
- Precificação: `backend/src/ISM.Application/Services/Menu/PricingCmvService.cs`
- Chat/propostas: `backend/src/ISM.Application/Services/Assistant/AssistantService.cs`
- Provider local: `backend/src/ISM.Infrastructure/AI/OllamaTextProvider.cs`
- API do Assistente: `backend/src/ISM.API/Controllers/Assistant/AssistantController.cs`
- Testes existentes: `backend/tests/ISM.UnitTests/AgentsTests.cs` e `backend/tests/ISM.IntegrationTests/AssistantApprovalTests.cs`
- Configuração atual do Assist: `docs/assistant/README.md`
