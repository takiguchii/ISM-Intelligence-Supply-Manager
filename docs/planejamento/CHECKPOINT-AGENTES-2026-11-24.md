# Checkpoint — correção do fluxo dos agentes ISM

**Data:** 24/11/2026

**Branch:** `fix/agentes-contexto-dados`

**Estado:** pausado a pedido do usuário para reiniciar a janela de contexto. Não retomar a implementação até o usuário enviar o prompt de retomada abaixo.

## Objetivo solicitado

Reformular o fluxo dos agentes para que eles entendam a pergunta, escolham e usem ferramentas autorizadas, analisem resultados reais e entreguem respostas simples e bem apresentadas. Cada especialista deve permanecer limitado ao próprio domínio; somente o assistente geral pode coordenar consultas de vários domínios. O usuário autorizou o desenvolvimento completo pelas fases 1–7 e pediu uma avaliação final com **20 prompts por agente**, incluindo investigação e correção das causas dos resultados ruins.

O usuário especificou que todas as cargas com modelo devem usar o **Qwen 3.5 4B local**, tanto na fase de desenvolvimento quanto na final. O modelo já está instalado no Ollama (`qwen3.5:4b`; Ollama 0.32.9), portanto não foi necessário baixá-lo.

## Regras e limites a preservar

- Não perder nem sobrescrever mudanças que já estavam na árvore de trabalho quando a tarefa começou.
- Não criar commits sem solicitação e aprovação explícitas do usuário, conforme `AGENTS.md`.
- O usuário autorizou o uso de `docker compose up -d --build` quando necessário. A stack Docker existente já estava ativa e ligada ao volume de desenvolvimento. Até agora não foi necessário reconstruir a stack; os testes integrados usam um banco EF InMemory isolado. Não usar `docker compose down -v`, reset de banco, prune ou limpeza de volumes.
- O pedido menciona “docker fetch” para limpeza. Esse não é um comando Docker padrão; não se inferiu que o usuário autorizou `docker system prune` ou limpeza semelhante.
- O ambiente deve continuar sendo monitorado durante a carga local: antes do piloto havia aproximadamente 10 GiB disponíveis; durante o Qwen 3.5 4B o uso estabilizou em cerca de 4,7–5,2 GiB disponíveis. O swap estava em torno de 4,1/8 GiB. As chamadas foram sequenciais porque o Ollama estava usando 100% de CPU.
- As respostas numéricas devem ser sustentadas pelos resultados das ferramentas; o modelo não deve calcular nem inventar fatos.
- Os dados da carga são sintéticos e isolados; não usar dados reais de restaurantes.

## Estado da árvore de trabalho antes da tarefa

No início havia mudanças locais pré-existentes (não criadas por esta tarefa):

- modificações em `backend/src/ISM.API/Extensions/ServiceCollectionExtensions.cs`;
- modificações em `backend/src/ISM.Application/Services/Agents/AgentIntentRouter.cs` e `ReadOnlyAgentTools.cs`;
- modificações em `backend/src/ISM.Application/Services/Assistant/AssistantService.cs`;
- modificações em `backend/tests/ISM.IntegrationTests/AssistantApprovalTests.cs` e `backend/tests/ISM.UnitTests/AgentIntentRouterTests.cs`;
- pasta `planejamento/` não rastreada.

Esses arquivos continham trabalho anterior relevante: ferramentas recentes de pedidos/estoque/cardápio, contexto amplo no chat e roteamento inicial. As mudanças desta tarefa foram aplicadas por cima dessas alterações sem resetar ou descartar arquivos.

## Fases e progresso

### Fase 1 — isolamento de especialidades no servidor

**Implementada parcialmente e testada em cenários direcionados.**

- Contexto factual do fallback passou a ser montado por especialista; financeiro recebe dados financeiros, estoque recebe dados de estoque, precificação recebe cardápio e o geral pode consolidar domínios.
- Alertas do fallback especializado são filtrados pelo tipo de domínio.
- Perguntas detectadas fora do domínio da thread são recusadas antes de executar ferramentas.
- Para threads especializadas, o histórico enviado ao modelo inclui mensagens do usuário, evitando reutilizar respostas antigas de outro fluxo.
- A execução no registry exige que a especialidade associada à chamada corresponda à ferramenta.
- Foi adicionado teste integrado que tenta perguntar sobre faturamento em uma thread de estoque e verifica que nenhuma informação financeira aparece.

**Pendência crítica para retomada:** o endpoint autenticado `POST /api/assistant/tools/execute` ainda recebe a especialidade no payload. O registry compara esse campo com a especialidade da ferramenta, mas o endpoint não vincula a chamada a uma thread nem valida a especialidade a partir de identidade de agente criada pelo servidor. Antes de concluir o isolamento, revisar se esse endpoint deve ser removido, internalizado ou alterado para verificar thread/escopo no servidor.

### Fase 2 — catálogo, contratos e roteamento de ferramentas

**Parcialmente implementada.**

- `AgentToolExecuteRequest` ganhou propriedade `Specialist`.
- chamadas internas do assistente geral, do contexto de vendas e do executor de orquestração passam a indicar o especialista correspondente;
- ferramentas de pedidos recentes foram alinhadas ao domínio financeiro; pedido mais recente é tratado como consulta geral;
- roteamento determinístico foi ampliado para reposição, simulação de preço, visão geral do estoque, comparações de fornecedor e períodos financeiros;
- comparação de fornecedor usa unidade e produto reconhecidos na pergunta, sem inventar `productId`;
- exceções de ferramenta são registradas com nome e correlation id em log, mantendo mensagem segura para o cliente.

### Fase 3 — análise da pergunta e seleção de ferramentas

**Implementada parcialmente.**

- o roteador determinístico tem prioridade;
- se não encontrar ferramenta, `ResolveIntentAsync` apresenta ao Qwen somente o catálogo permitido à especialidade da conversa;
- a resposta JSON passa pelo validador estruturado (`ResolveStructured`), allowlist, schema, identidade da ferramenta e checagem de especialidade antes da execução;
- argumentos de tenant não são aceitos do modelo;
- o classificador de domínio bloqueia consultas cruzadas em threads especializadas.

Ainda falta testar o planner Qwen em paráfrases para todos os agentes e considerar perguntas ambíguas que exigem esclarecimento em vez de tentativa de inferência.

### Fase 4 — resposta final clara e fundamentada

**Parcialmente implementada; a validação numérica final ainda não foi rodada após a última edição.**

- respostas de ferramentas foram reformuladas em texto português, com itens e quebras de linha, valores monetários pt-BR e indicação de período/fonte quando aplicável;
- o sintetizador usa Qwen local depois da consulta e recebe somente resultado/fatos da ferramenta;
- instruções específicas evitam apresentar histórico de compra como cotação externa atual;
- foi acrescentada uma validação que recusa sínteses com números não presentes nos fatos verificados e retorna a resposta determinística formatada nesse caso.

### Fase 5 — assistente geral/orquestração

**Parcialmente implementada.**

- orquestração continua com limites determinísticos de etapas, concorrência e orçamento;
- resultados são sintetizados com formatador humano em vez de JSON bruto;
- a origem da resposta registra os nomes das ferramentas executadas;
- síntese Qwen é feita depois das consultas, e a resposta mantém status parcial/avisos.

O planejador atual ainda usa regras determinísticas limitadas. A revisão cruzada estruturada entre especialistas prevista no plano anterior não foi implementada; só deve ser adicionada se os testes mostrarem benefício e respeitar limite de uma rodada.

### Fase 6 — interface

**Ainda não alterada.**

O drawer já mostra nome do especialista, origem e estado de consulta, e o texto usa `whitespace-pre-wrap`, então as listas em linhas já aparecem. Revisar com a resposta final do Qwen se é necessária apresentação adicional. Não há alteração frontend nesta etapa.

### Fase 7 — validação e carga

**Harness separado criado; piloto incompleto/abaixo da meta; carga final de 20 por agente ainda não executada.**

- criado `backend/tests/ISM.AgentLoadTests/ISM.AgentLoadTests.csproj`, fora da solução principal para que 100 inferências não rodem em cada suíte normal;
- `AgentLocalModelLoadTests` inicializa `WebApplicationFactory` com Qwen `qwen3.5:4b`, timeout 240s e banco EF InMemory aleatório;
- fixture sintética inclui produto com estoque e movimento, prato e ingrediente, dois históricos de fornecedor, despesas e pedido pago;
- cada cenário faz chamada HTTP autenticada às threads reais, executa ferramentas reais da aplicação e usa Qwen para síntese;
- os casos registram agente, prompt, ferramenta esperada, origem/ferramenta observada, resposta, modo, latência, classe de falha; reporta também média e p95;
- a carga final exige 20 prompts para cada uma das cinco especialidades testadas: `stock`, `pricing`, `suppliers`, `finance`, `general` (100 cenários no total). Casos fora de escopo verificam recusa; casos positivos verificam ferramenta, fato-chave, legibilidade e síntese local.

### Carga de desenvolvimento executada

Comando usado no primeiro piloto (4 amostras por agente, 20 respostas no total):

```bash
ISM_AGENT_LOAD_PROMPT_LIMIT=4 dotnet test backend/tests/ISM.AgentLoadTests/ISM.AgentLoadTests.csproj --no-restore --filter FullyQualifiedName~Each_agent_handles_twenty_real_tool_and_synthesis_prompts_with_qwen35_4b -v normal
```

Resultado registrado em [P7-carga-qwen3.5-4b.json](relatorios/P7-carga-qwen3.5-4b.json): **16/20 PASS**.

| Especialista | Resultado piloto |
|---|---:|
| stock | 4/4 |
| pricing | 4/4 |
| suppliers | 1/4 |
| finance | 3/4 |
| general | 4/4 |

Diagnóstico das quatro falhas:

1. `suppliers`: Qwen encurtou “Fornecedor Alpha” para “Alpha”; avaliação estava estrita demais com o nome completo, sem erro de consulta nem número.
2. `suppliers`: respostas chamaram o histórico de compra de oferta recente/atual; a instrução do sintetizador foi reforçada para explicitar que são preços históricos e que não houve cotação externa.
3. `suppliers`: “Mostre quem vende arroz de carga em kg” não era roteado. Parser de paráfrases foi ampliado e foram adicionados testes de roteador.
4. `finance`: “Qual foi o faturamento deste mês?” acionou `finance.getPeriodSummary`, que respondeu com vendas, despesas e fluxo do período, mas o teste esperava `finance.getRevenueSummary`. O caso foi corrigido para aceitar a ferramenta de período, que é apropriada para a pergunta.

Depois dessas correções foi iniciada uma segunda execução parcial, limitada a `suppliers,finance`:

```bash
ISM_AGENT_LOAD_PROMPT_LIMIT=4 ISM_AGENT_LOAD_SPECIALISTS=suppliers,finance dotnet test backend/tests/ISM.AgentLoadTests/ISM.AgentLoadTests.csproj --no-restore --filter FullyQualifiedName~Each_agent_handles_twenty_real_tool_and_synthesis_prompts_with_qwen35_4b
```

Essa execução foi cancelada a pedido do usuário quando o teste ainda estava processando as inferências. **Não há resultado para essa segunda rodada**; o JSON permanece sendo o relatório do primeiro piloto. O processo de teste foi interrompido e não há inferência de carga em execução.

## Validações já executadas

- `dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore`: passou com 0 warnings e 0 erros depois das alterações de segurança e novamente depois das últimas mudanças de roteamento.
- `dotnet test backend/tests/ISM.UnitTests/ISM.UnitTests.csproj --no-restore --filter FullyQualifiedName~AgentIntentRouterTests`: passou, primeiro com 16 e depois com 18 casos, incluindo paráfrases adicionadas.
- `dotnet test backend/tests/ISM.IntegrationTests/ISM.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~Assistant_ToolExecution_RejectsToolFromAnotherSpecialist|FullyQualifiedName~Assistant_StockThread_RejectsFinancialQuestionWithoutReturningFinancialFacts|FullyQualifiedName~Assistant_ToolExecution_ReturnsEvidenceAndCorrelationForAuthorizedRead'`: 3 passaram.
- `dotnet build backend/tests/ISM.AgentLoadTests/ISM.AgentLoadTests.csproj --no-restore`: passou antes das últimas adições ao harness; recompilado automaticamente na segunda rodada, mas a rodada foi cancelada durante a execução.
- `npm run --prefix frontend build`: ainda não executado.
- suíte completa `dotnet test backend/ISM.sln`: ainda não executada.
- carga final 20 × 5 agentes: ainda não executada.

## Arquivos criados/alterados por esta tarefa

Alterações incrementais feitas em arquivos que já estavam modificados antes do início: `ServiceCollectionExtensions.cs`, `AgentIntentRouter.cs`, `ReadOnlyAgentTools.cs`, `AssistantService.cs`, `AssistantApprovalTests.cs`, `AgentIntentRouterTests.cs`.

Arquivos limpos antes da tarefa e modificados por esta execução: `AgentContracts.cs`, `AgentToolRegistry.cs`, `ConstrainedOrchestrationService.cs`.

Novos artefatos desta execução: `backend/tests/ISM.AgentLoadTests/`, `docs/planejamento/relatorios/P7-carga-qwen3.5-4b.json` e este checkpoint. A pasta `planejamento/` já estava não rastreada antes da tarefa; preservar seus arquivos.

Não houve alterações frontend, migration, commit ou reconstrução do Docker.

## Próxima sequência de trabalho ao retomar

1. Ler este checkpoint e conferir `git status --short` antes de editar; preservar todas as mudanças atuais e a pasta `planejamento/`.
2. Rodar o piloto direcionado corrigido para `suppliers,finance` e conferir o JSON atualizado; classificar as falhas como roteamento, fatos, isolamento, formatação ou síntese.
3. Se ainda houver falhas, corrigir uma causa de cada vez, rodar testes de roteador/build e repetir o piloto até resultados aceitáveis. Não mudar expectativas para esconder erro factual.
4. Revisar e fechar a pendência crítica do endpoint `POST /api/assistant/tools/execute`, confirmando que chamadas de ferramenta especializadas são autorizadas pelo servidor, não apenas por uma propriedade do payload.
5. Rodar os testes direcionados de integração e unidade novamente.
6. Executar a carga completa sequencial com 20 prompts por `stock`, `pricing`, `suppliers`, `finance` e `general`, usando somente Qwen 3.5 4B. Monitorar `free -h`, `ollama ps` e, se necessário, `docker stats`; interromper se memória/swap degradarem de forma perigosa.
7. Analisar cada caso reprovado, corrigir causa-raiz e repetir a carga afetada. O usuário pediu investigação/correção até respostas decentes; preservar no relatório os resultados antes/depois. Não aceitar mais que 3 ciclos para a mesma causa sem registrar o bloqueio e explicá-lo.
8. Revisar UX do chat e implementar ajustes frontend se a carga mostrar necessidade; então executar `npm run --prefix frontend build`.
9. Executar suíte completa backend e builds pertinentes; documentar PASS/FAIL/BLOCKED/SKIP sem afirmar validação que não foi feita.
10. Atualizar relatório e entregar resumo ao usuário. Não fazer commit sem aprovação explícita.

## Prompt de retomada

Copie e cole esta mensagem após `/clear`:

> Retome a tarefa de correção completa dos agentes ISM a partir de `docs/planejamento/CHECKPOINT-AGENTES-2026-11-24.md`. Leia o checkpoint e confira a árvore de trabalho antes de qualquer edição; preserve as alterações preexistentes e não faça commit. Continue pela segunda carga piloto de fornecedores/financeiro com o Qwen 3.5 4B local, corrija causas reais, feche a autorização server-side do endpoint direto de ferramentas e então conclua as fases 1–7. Ao final execute 20 prompts por agente (`stock`, `pricing`, `suppliers`, `finance`, `general`), analise e corrija falhas, monitore RAM/swap e produza o relatório final. Não use `docker prune` nem apague volumes; use `docker compose up -d --build` apenas se necessário e mantendo isolamento dos dados existentes. `docker fetch` não foi interpretado como autorização para limpeza destrutiva.

## Atualização de retomada — 25/11/2026

A retomada solicitada foi concluída. As fases 1–7 estão encerradas. As alterações e arquivos preexistentes foram preservados; nenhum commit foi criado.

- Fechado o endpoint direto de ferramentas com vínculo à thread e autorização calculada no servidor.
- Corrigidos roteamento e síntese após piloto de fornecedores/financeiro e análise caso a caso da primeira carga completa.
- Carga inicial: 20 prompts por especialista, 79/100. Retentativa dos 21 casos reprovados: 21/21. Resultado consolidado: 100/100 (20/20 por especialista). O relatório registra respostas originais e finais e marca respostas que precisaram do formatador determinístico.
- Suíte completa `dotnet test backend/ISM.sln --no-restore -v q`: PASS, 332 testes (278 unitários, 53 integração, 1 arquitetura).
- Build frontend `npm run --prefix frontend build`: PASS, com avisos de dependências/chunk reportados no relatório.
- Não houve alteração de UI; a inspeção confirmou que o drawer já apresenta especialista, fonte e quebras de linha.
- RAM/swap foram monitoradas; pico observado de aproximadamente 5,8 GiB disponíveis e 5,0/8 GiB de swap. Não houve comando de limpeza/descarregamento do Ollama, operação Docker, prune ou remoção de volume nesta retomada.
- Os modelos Groq solicitados foram bloqueados antes de inferência por HTTP 403 Cloudflare 1010; nenhuma tentativa adicional foi feita.

Relatórios finais: [RELATORIO-FINAL-AGENTES.md](relatorios/RELATORIO-FINAL-AGENTES.md) e [P7-relatorio-final-qwen3.5-4b.json](relatorios/P7-relatorio-final-qwen3.5-4b.json).
