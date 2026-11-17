# Relatório P6 — Validação final do ISM Assist

Data: 17/11/2026
Branch: `feat/ism-assist-v3-mascot-motion`

## Resultado executivo

**Status: PASSOU nos gates automatizados e de ambiente executados; validação final formal permanece NÃO CONCLUÍDA para os itens manuais não executados.**

Não foi declarado `PASSOU` para cenários que exigiriam amostragem manual, duas contas reais independentes ou inspeção visual humana.

### Percentuais consolidados

Não existe um percentual único honesto para a P6, pois os gates têm denominadores diferentes. Os percentuais abaixo usam somente denominadores comprovados:

| Indicador | Cálculo | Resultado |
|---|---:|---:|
| Oráculos fixos obrigatórios | 125 aprovados / 125 | **100%** |
| Regressão automatizada | 239 aprovados / 239 | **100%** |
| Provider degradado | 3 comportamentos aprovados / 3 | **100%** |
| Perfis exercitados | manager + chef aprovados / 2 | **100%** |
| Tenants nas fixtures A/B | 2 fixtures isoladas / 2 | **100%** |
| Automação P5 validada | 5 cenários específicos / 5 | **100%** |
| Carga contínua local | 6.097 respostas HTTP 200 / 6.097 | **100%** |
| Variações exploratórias manuais | 0 perguntas / 375 previstas | **0%** |
| Contas reais independentes | 1 tenant seed / 2 necessários | **50%** |

## Evidências executadas

| Área | Resultado | Evidência |
|---|---|---|
| 125 cenários obrigatórios | PASSOU | 40 cenários AGIR + 60 NÃO AGIR dos especialistas e 25 cenários do coordenador; suíte integral verde |
| Regressão backend | PASSOU | 194 unitários + 44 integração + 1 arquitetura = 239 testes, 0 falhas |
| Provider desabilitado | PASSOU | fallback honesto via integração e teste de resiliência |
| Provider HTTP degradado | PASSOU | teste direto com HTTP 503, sem exceção vazando |
| Provider lento/timeout | PASSOU | teste direto com cancelamento simulado, retorno `Timeout` controlado |
| Provider real | PASSOU | Ollama local `gemma3:270m`, chamada `/api/chat` HTTP 200 e chat da API com `mode=llm` |
| API, frontend e banco Docker | PASSOU | containers saudáveis, Swagger HTTP 200, frontend HTTP 200, migration P5 aplicada e 27 tabelas presentes |
| Perfis | PASSOU | manager autorizado; chef recebeu 403 no endpoint protegido |
| Isolamento de tenant A/B | PASSOU em integração | fixtures independentes A/B, dados cruzados rejeitados e ausência de vazamento na suíte de integração |
| Concorrência, dedupe e histórico de automação | PASSOU | integração P5 e testes unitários do runner |
| Alertas e dedupe | PASSOU | cobertura existente da suíte e execução integral sem falhas |
| Correção do loading contínuo | PASSOU | polling não substitui alertas idênticos; refresh mantém conteúdo existente; build frontend concluído |
| Smoke visual da aplicação | PASSOU parcialmente | Chrome headless abriu `/login` e o dashboard autenticado em viewport 1440×1000; conteúdo permaneceu visível após 7 s, Assist aparece no canto inferior e não houve tela de loading contínuo |
| Desempenho observado | PASSOU como smoke | 20 amostras: frontend p50 52,7 ms / p95 74,7 ms; Swagger p50 166,8 ms / p95 245,4 ms; status p50 13,3 ms / p95 15,7 ms |
| Concorrência observada | PASSOU como smoke | 100 requisições concorrentes em 10 workers para `/api/system/status`: 100 HTTP 200, p50 48,76 ms, p95 489,84 ms, máximo 500,71 ms |
| Carga curta observada | PASSOU como smoke | 1.000 requisições em 20 workers: 1.000 HTTP 200, 0 erros, p50 85,08 ms, p95 121,01 ms, máximo 214,84 ms; backend 12,34% CPU e 632,6 MiB durante a amostra |
| Amostra contínua local | PASSOU como smoke | 30 segundos, 6.097 requisições em 20 workers: 6.097 HTTP 200, 0 erros, p50 70,57 ms, p95 113,13 ms, máximo 219,47 ms; após a amostra backend 12,42% CPU/431,8 MiB e MySQL 0,88% CPU/458,2 MiB |
| Tokens e pool observados | PASSOU como smoke | Ollama real: 15 tokens de prompt + 3 de saída, duração total 4,121 s; MySQL: 2 conexões/2 em execução, máximo observado 11, limite configurado 200 |

## Itens não executados como validação manual formal

| Item | Status | Motivo |
|---|---|---|
| Três variações de pergunta para cada um dos 125 casos | NÃO EXECUTADO | os 125 casos foram validados pelos oráculos automatizados; não foi feita a matriz manual de 375 perguntas |
| Duas contas A/B reais independentes | NÃO EXECUTADO | o ambiente Docker possui apenas o tenant seed 1; a validação A/B executada usa fixtures reversíveis da suíte de integração |
| Regressão visual V3 com inspeção humana | NÃO EXECUTADO | houve inspeção assistida de um viewport autenticado no tema escuro, mas não uma matriz completa de rotas, temas e breakpoints |
| Carga sustentada e taxa de ferramenta | NÃO EXECUTADO | foi executada uma carga curta de 1.000 requisições e observados tokens/pool pontualmente, mas não um ensaio sustentado com instrumentação contínua de pool e taxa de ferramenta |

## Verificações de código e rollback

## Matriz de rastreabilidade

| Gate do guia P6 | Fonte verificável | Situação atual |
|---|---|---|
| 40 cenários AGIR dos especialistas | `SpecialistAgentScenarioTests.Scenarios()` e teoria `P2_specialists_execute_or_refuse_safely` | PASSOU, 10 por especialista |
| 60 cenários NÃO AGIR dos especialistas | mesma teoria, incluindo input ausente, inválido, cross-tenant e dados insuficientes | PASSOU, 15 por especialista |
| 25 cenários do agente-pai | `AssistantIntentCoordinatorTests.Scenarios()` e teoria `Parent_agent_routes_only_safe_domains` | PASSOU |
| Regressão de API, banco e autorização | `dotnet test backend/ISM.sln --no-restore` e testes de integração | PASSOU, 239/239 |
| Automação segura | `AssistantApprovalTests` e `AutomationSafetyTests` | PASSOU, dry-run, dedupe, expiração, pausa, revogação e isolamento |
| Provider degradado | `AssistantProviderResilienceTests` e `AssistantApprovalTests` | PASSOU, desabilitado, HTTP 503 e timeout |
| Provider real e Docker | smoke Ollama, Docker Compose, Swagger, frontend e migration MySQL | PASSOU |
| Carga e desempenho | amostras de 20, 100, 1.000 e 6.097 requisições | PASSOU como observação local |
| Visual V3 completo | screenshots autenticados parciais, sem matriz completa | NÃO CONCLUÍDO |
| Três variações exploratórias por caso | não há execução documentada da matriz manual de 375 perguntas | NÃO CONCLUÍDO |

- `git diff --check`: PASSOU.
- Logs do backend nos últimos 15 minutos: nenhum `error`, `exception`, `timeout` ou `failed` detectado.
- A infraestrutura registra `AddDbContext<IsmDbContext>` (não `AddDbContextPool`) e não expõe contador de pool; o registry retorna duração por ferramenta, mas não possui agregador de taxa/percentis em runtime.
- `dotnet test backend/ISM.sln --no-restore`: PASSOU.
- Build backend: PASSOU, sem erros.
- Build frontend com `NUXT_IGNORE_LOCK=1`: PASSOU.
- Migration de automações: aplicada no MySQL Docker.
- Nenhuma chamada de automação altera dados externos: o runner P5 permanece em `DryRun`/`Simulated`.
- Rollback lógico disponível por pausa e revogação da política; alterações de schema podem ser revertidas pela migration correspondente antes de publicação.

## Limites e observações

- O endpoint `/health` não existe neste projeto; disponibilidade foi verificada por Swagger, frontend, `/api/system/status`, MySQL e chamada real ao provider.
- Os warnings já existentes do EF Core sobre filtro global de `Dish` e os avisos de bundle/Browserslist não causaram falhas, mas permanecem registrados para uma etapa futura de limpeza técnica.
- As alterações P5, a correção do loading, os testes P6 e este relatório fazem parte da entrega versionada solicitada.

## Conclusão

Os gates automatizados, a regressão, o provider real e a integração Docker estão verdes. A P6 não deve ser apresentada como validação final 100% formal enquanto os quatro itens manuais acima permanecerem não executados.
