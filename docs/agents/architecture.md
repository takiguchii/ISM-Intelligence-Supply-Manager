# Arquitetura oficial dos agentes ISM

## Objetivo e fonte de verdade

Os agentes do ISM transformam dados do restaurante em métricas, alertas e propostas explicáveis. O banco, as regras determinísticas e os contratos de ferramenta são a fonte de verdade; um modelo de linguagem pode interpretar e redigir, mas não calcula valores críticos por conta própria, não executa SQL e não altera dados diretamente.

```mermaid
flowchart LR
    D[Dados do tenant] --> R[Regra determinística]
    R --> A[Alerta ou recomendação]
    A --> P[Proposta com evidências]
    P -->|aprovação conforme risco| X[Executor transacional]
    X --> AU[Auditoria e atualização]
    C[Chat] --> T[Catálogo de ferramentas autorizadas]
    T --> D
```

## Agentes implementados

| Agente | Responsabilidade | Saída | Escrita automática |
|---|---|---|---|
| `stock` | Saldo, mínimo, consumo e risco de ruptura | Alerta e sugestão de reposição | Não movimenta estoque |
| `pricing` | Custo, CMV, margem e preço sugerido | Métrica e proposta de preço | Somente após aprovação |
| `suppliers` | Histórico e comparação de ofertas | Comparação e alerta de preço | Não envia cotação |
| `assistant` | Contexto, propostas e conversas | Resposta, encaminhamento e auditoria | Nunca por texto livre |

O scheduler executa os agentes operacionais por restaurante. Cada execução deve ser independente: uma falha de provider ou de um restaurante não pode interromper os demais.

## Contratos de ferramenta

As ferramentas são registradas no backend por `IAgentToolRegistry`. Cada descriptor declara domínio, versão, operação, papéis permitidos, limite, risco e schema de entrada. O contexto de execução contém `UserId`, `RestaurantId`, papel e correlação; o restaurante não é aceito apenas como texto do prompt.

Ferramentas de leitura disponíveis:

| Ferramenta | Domínio | Uso |
|---|---|---|
| `stock.getProduct` | estoque | Consultar saldo, mínimo, custo e risco |
| `stock.analyzeReplenishment` | estoque | Simular reposição sem comprar |
| `pricing.getDishCost` | precificação | Consultar preço, custo e revisão |
| `pricing.simulateDishPrice` | precificação | Simular margem e preço sem alterar cardápio |
| `suppliers.compareOffers` | fornecedores | Comparar histórico equivalente sem enviar cotação |
| `finance.getRevenueSummary` | financeiro | Consultar resumo do mês corrente |
| `finance.getPeriodSummary` | financeiro | Consultar período e fluxo de caixa |
| `assistant.getAlertContext` | assistente | Consultar fatos de um alerta do tenant |

Toda resposta estruturada contém decisão, dados, evidências, correlação e mensagem. Ausência de dados é retornada explicitamente; não há fallback silencioso que invente números.

## Chat e aprovação

O chat geral roteia intenção para ferramentas de leitura. O chat contextual preserva alerta, entidade, snapshot, thread, usuário e restaurante. Uma ação mutável percorre:

1. cálculo determinístico;
2. proposta com snapshot e evidências;
3. aprovação conforme papel e risco;
4. verificação de concorrência e idempotência;
5. execução transacional;
6. auditoria.

Chef e garçom não acessam ações administrativas. Pagamentos, exclusões, comunicação externa e compromisso automático com fornecedor permanecem bloqueados para automação.

## Isolamento e observabilidade

Todas as consultas e escritas são tenant-scoped pelo backend e pelo filtro global do `IsmDbContext`. Execuções, propostas e mutações devem ser correlacionáveis por `runId`/correlation id, usuário, versão do agente, horário, evidências e resultado. Segredos nunca são enviados ao frontend nem ao modelo.

## Provedores de IA

O domínio depende de uma abstração de provider. Ollama é o provider local atual; outros providers compatíveis podem ser adicionados sem alterar as regras dos agentes. Multimodalidade é uma capacidade (`text`, `image`, `audio` ou `web`), não uma dependência do caminho determinístico. Timeout, indisponibilidade ou resposta malformada devem produzir fallback explícito e observável.

## Referências de implementação

- Contratos: `backend/src/ISM.Application/Agents/AgentContracts.cs`
- Ferramentas: `backend/src/ISM.Application/Services/Agents/`
- Scheduler: `backend/src/ISM.API/HostedServices/AgentsBackgroundScheduler.cs`
- Assistente: `backend/src/ISM.Application/Services/Assistant/AssistantService.cs`
- Provider local: `backend/src/ISM.Infrastructure/AI/OllamaTextProvider.cs`
- API: `backend/src/ISM.API/Controllers/Assistant/AssistantController.cs`
