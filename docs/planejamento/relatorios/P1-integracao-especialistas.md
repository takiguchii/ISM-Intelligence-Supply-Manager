# P1 — Integração integral dos especialistas

## Identificação

- Branch: `feat/assistant-orquestracao-p0`
- Base: `develop` em `351f1c8`
- Fase: P1
- Data: `2026-11-23`
- Commit: pendente de aprovação explícita; não houve commit automático.
- Notificação Fors/Telegram: `BLOCKED`; variáveis seguras ausentes e o token fornecido na conversa não foi utilizado.

## Entrega realizada

### Backend

- Criada lista canônica no Domain: `general`, `stock`, `pricing`, `suppliers`, `finance`.
- `AssistantThread` passou a persistir `SpecialistId`, `SourceView` e `SubjectEntityType`, mantendo `SubjectEntityId` existente.
- `AssistantThreadResponse` retorna a identidade e o contexto estrutural.
- `AssistantCreateThreadRequest` aceita especialista e metadados de tela/entidade sem aceitar restaurante no payload.
- Threads antigas recebem `general` pela migration aditiva; não há reassignment silencioso no código de leitura.
- O servidor deriva o especialista de alertas legados somente quando o cliente não envia identidade; quando envia, exige compatibilidade com o alerta.
- Entidades contextuais sem alerta são revalidadas por repositório e tenant (`Product`, `Dish`, `Supplier`).
- Mensagens em thread especializada bloqueiam intenções de outro domínio antes da execução de ferramenta.
- Criada migration `20261124010135_AddAssistantThreadSpecialistIdentity`, com default seguro `general` e índice tenant/usuário/especialista/data.

### Frontend

- `AgentKind` agora inclui `finance`.
- `useAssistantUi` passou a expor `openSpecialistChat` e mantém compatibilidade com `openChat`/`openGeneralChat`.
- `useAssistantChat` cria/restaura thread por especialista e entidade, com controle de revisão contra cross-talk.
- `AssistantChatDrawer` usa `thread.specialistId` retornado pelo servidor para identidade visual e cabeçalho.
- Financeiro abre `finance`, usa mascote financeiro e CTA `Conversar com o agente`.
- Cardápio abre `pricing` com `Dish` e ID autorizado.
- Estoque abre `stock` com `Product` e ID autorizado mesmo sem alerta.
- Fornecedores abre `suppliers` mesmo sem alerta; mantém contexto do alerta quando presente.
- Dashboard mantém os três cards e abre o especialista do card quando não existe alerta.
- A paleta do mascote ganhou variação de `finance` para evitar fallback visual indefinido.

## Testes e evidências

| Comando | Resultado |
|---|---|
| `dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore` | PASS — 0 erros; warnings preexistentes de `Id` oculto foram observados em uma execução anterior, build final sem warnings novos |
| `dotnet test backend/tests/ISM.IntegrationTests/ISM.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~AssistantApprovalTests` | PASS — 22/22 |
| `dotnet test backend/ISM.sln --no-restore` | PASS — Architecture 1/1, Unit 213/213, Integration 51/51; total 265/265 |
| `npm run --prefix frontend build` | PASS — Nuxt/Nitro concluído |
| Testes frontend `waiter`, `system-alerts`, `kitchen`, `confirmation`, `cash` | PASS — 32/32 |
| `git diff --check` | PASS |
| `dotnet ef migrations add ...` | PASS — migration gerada; não aplicada em banco compartilhado |

Uma tentativa inicial executou build backend e testes em paralelo e produziu uma disputa transitória de arquivo de referência do .NET. O teste foi repetido serialmente e passou; isso não foi classificado como falha funcional.

## Segurança validada

- `RestaurantId` continua vindo de `ICurrentUser`.
- `AgentToolRegistry` mantém allowlist, RBAC, deadline e rejeição de `restaurantId` no input.
- Contexto de alerta é validado pelo restaurante autenticado.
- Entidade de thread sem alerta não pode apontar para registro de outro restaurante.
- Thread de estoque recusou uma pergunta financeira sem executar ferramenta.
- Especialista `finance` não foi representado como `pricing` no CTA ou no mascote.

## Limitações e bloqueios honestos

- A migration foi gerada e compilada, mas não foi aplicada em banco MySQL descartável nesta execução. Os containers `ism-*` existentes e o disco em 89% tornam inadequado aplicar migration no ambiente compartilhado sem configurar um projeto/volume isolado explicitamente.
- Não foi realizado teste de navegador/E2E visual nesta fase; o build e os contratos foram validados, mas a confirmação de comportamento visual em cada viewport ainda pertence ao ciclo de UX/regressão.
- Notificação Fors bloqueada até rotação do token exposto e configuração externa de variáveis seguras. Nenhum segredo foi armazenado no relatório ou no Git.

## Gate P1

`PASS` para implementação, contratos, autorização, migration gerada e testes automatizados.

`BLOCKED` para aplicação da migration em banco descartável e aviso Telegram, por dependências de ambiente seguro ainda não configuradas.

Não iniciar P2 nem criar commit desta fase até o usuário aprovar o commit e, se necessário, disponibilizar um banco de teste isolado. O próximo passo seguro é executar a migration em um projeto Docker com slug/volume próprio, sem tocar nos containers ou volumes atuais.
