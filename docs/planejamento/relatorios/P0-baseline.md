# P0 — Baseline auditável do ISM Assist

## Identificação

- Branch de origem: `develop`
- Commit de origem: `351f1c8` (`feat: separa perfil de caixa dos administradores`)
- Branch de trabalho: `feat/assistant-orquestracao-p0`
- Data da execução: `2026-11-23`
- Diretório de planejamento encontrado: `planejamento/` (não rastreado na branch); o plano foi preservado sem mover ou sobrescrever arquivos.
- Estado inicial sujo: `?? planejamento/`
- Commit desta fase: pendente de aprovação explícita; nenhum commit automático foi criado.

## Escopo e segurança

O P0 foi executado como diagnóstico e baseline. Não houve alteração de banco, reset de volume, `git reset`, `docker compose down -v`, instalação de dependência global ou configuração de segredo.

As credenciais de Telegram presentes na solicitação não foram gravadas, impressas, versionadas ou utilizadas. As variáveis `AUDREY_BOT_TOKEN`, `AUDREY_CHAT_ID`, `TELEGRAM_BOT_TOKEN` e `TELEGRAM_CHAT_ID` não estão configuradas no executor. Status de notificação: `BLOCKED`. Como o token foi exposto na conversa, a configuração definitiva exige rotação prévia.

## Ambiente validado

| Componente | Resultado |
|---|---|
| .NET SDK | 8.0.132 |
| .NET runtime | 8.0.32 |
| Node.js | v22.22.2 |
| npm | 10.9.7 |
| Docker Compose | 5.6.0 |
| Ollama | 0.32.9 instalado; serviço de usuário `inactive` |
| Memória observada | 17 GiB total, 8.8 GiB disponível, 4 GiB de swap usado |
| Disco `/home` | 236 GiB total, 207 GiB usado, 26 GiB disponíveis (89%) |
| Docker | 6 containers já ativos; 17 volumes; 1.542 GB de build cache |

O compose foi validado com `docker compose config --quiet`. O serviço não foi reiniciado nesta fase porque já existem containers ativos e o baseline automatizado não depende deles. A limpeza de cache/volumes fica condicionada a necessidade operacional; volumes de dados não serão removidos.

## Baseline de testes

### Backend

Comando: `dotnet test backend/ISM.sln --no-restore`

Resultado: `PASS` — 263 testes executados, 0 falhas.

- ArchitectureTests: 1/1
- UnitTests: 213/213
- IntegrationTests: 49/49

Warnings observados, sem falha no baseline:

- entidades `Category`, `Product`, `Plan`, `Dish`, `Supplier` e `Restaurant` ocultam `BaseEntity.Id`;
- filtro global EF de `Dish` em relação obrigatória com `DishIngredient`;
- logs esperados de cenários negativos de autorização/concorrência nos testes de integração.

### Frontend

Comandos:

```text
npm ci --prefix frontend
npm run --prefix frontend build
npm run --prefix frontend test:waiter
npm run --prefix frontend test:system-alerts
npm run --prefix frontend test:kitchen
npm run --prefix frontend test:confirmation
npm run --prefix frontend test:cash
```

Resultado: `PASS` — build Nuxt concluído e 32 testes unitários executados (7 + 2 + 7 + 4 + 12), sem falhas.

Warnings observados:

- dependências npm deprecated durante `npm ci`;
- `npm audit` reportou 36 vulnerabilidades (3 low, 11 moderate, 16 high, 6 critical); não foi aplicado `npm audit fix` no P0;
- Browserslist desatualizado;
- chunks acima de 500 kB, incluindo um chunk de aproximadamente 1.25 MB.

## Inventário funcional real

### Backend

- `AssistantController` expõe status, overview, ações, ferramentas, threads, contexto de alerta e mensagens; a política da API exige `RestaurantManagerOrAbove`.
- `AssistantService` deriva o restaurante de `ICurrentUser`, valida `OriginAlertId` pelo restaurante e usa o repositório de threads com `(RestaurantId, UserId)`.
- `AssistantThread` persiste `RestaurantId`, `UserId`, `ParentThreadId`, `OriginAlertId`, `OriginType`, `SubjectEntityId`, `Subject`, snapshot e timestamps. Não possui `SpecialistId`, `SourceView` ou `SubjectEntityType` dedicado.
- `AssistantCreateThreadRequest` aceita somente `OriginAlertId`, `ParentThreadId` e `Subject`.
- `AssistantThreadResponse` não retorna especialista nem metadados de origem de tela/entidade além do contexto do alerta.
- `IsmDbContext` aplica filtro tenant às threads e mensagens e possui índices por restaurante/usuário/alerta.
- `AgentToolRegistry` aplica escopo autenticado, papel, allowlist, deadline e rejeita `restaurantId` enviado na entrada.
- `AgentIntentRouter` possui roteamento determinístico parcial e schema de ferramentas para `finance`, `stock`, `pricing`, `suppliers` e contexto de alerta; isso não substitui uma identidade fixa por thread.
- `AssistantIntentCoordinator` usa o contexto para escolher ferramentas de estoque, precificação, fornecedores ou alerta.

### Frontend

- `frontend/types/agentExperience.ts` define `AgentKind` como `stock | pricing | suppliers | general`; não inclui `finance`.
- `useAssistantUi` expõe `openChat(alert)`, `openGeneralChat()` e estado global do drawer, sem `openSpecialistChat`.
- `useAssistantChat.ensureReady` cria/restaura por `originAlertId`; a requisição usa revisão para evitar respostas obsoletas, mas não passa especialista.
- `AssistantChatDrawer.vue` deriva o mascote pela presença/contexto do alerta (`agentForAlert`) e usa `general` sem contexto. A identidade visual não é a identidade autorizada pelo servidor.
- Dashboard (`pages/index.vue` + `useDashboardMetrics` + `AiInsightCard`) já agrega exatamente três cards (`stock`, `pricing`, `suppliers`) pela fonte única `useSystemAlertCenter`.
- `useSystemAlertCenter` centraliza polling de 45 s, baseline da sessão, deduplicação, cooldown de 60 s, leitura e descarte.
- Financeiro usa `openGeneralChat`, exibe mascote `pricing` e o CTA `Consultar resumo mensal`; há gap de identidade real `finance`.
- Cardápio abre `openGeneralChat` para custo/preço do prato; não passa `pricing` explícito.
- Estoque abre alerta contextual quando existe, mas sem alerta abre `openGeneralChat`; deve ser `stock` explícito.
- Fornecedores só abre conversa quando existe `supplierAlert`; precisa permitir `suppliers` sem alerta.
- O launcher global abre `general`, conforme esperado.
- O componente visual `AgentMascot2D` já é SVG 2D reutilizável; não há necessidade de reconstruí-lo no P1.

## Divergências em relação ao plano/snapshot

1. A documentação P0 dizia que `SubjectEntityId` não existia; ele já está presente no código atual. Ainda faltam especialidade e metadados de origem.
2. O frontend já tem agregação de três cards, centro de alertas com cooldown e testes de alertas; o trabalho deve evoluir contratos, não duplicar polling ou criar outro centro.
3. O backend já possui `finance` em ferramentas e testes de consulta financeira, mas a UX de thread ainda não expõe `finance` como identidade canônica.
4. O compose usa `gemma3:270m` por padrão, enquanto Ollama local possui vários modelos, incluindo `qwen3.5:4b`; não foi feita seleção de modelo no P0.
5. A pasta `planejamento/` está fora de `docs/planejamento/` e não rastreada; o relatório foi criado no caminho prescrito `docs/planejamento/relatorios/` sem modificar a pasta existente.

## Arquivos e contratos candidatos para P1

### Backend

- `ISM.Domain/Modules/Assistant/Entities/AssistantThread.cs`: identidade canônica e metadados persistidos.
- `ISM.Application/DTOs/Assistant/AssistantDtos.cs`: request/response compatíveis e validação de valores permitidos.
- `ISM.Application/Services/Assistant/AssistantService.cs`: criação, restauração, contexto, autorização e mapeamento da thread.
- `ISM.Domain/Interfaces/Assistant/IAssistantConversationRepository.cs` e `ISM.Infrastructure/Repositories/Assistant/AssistantConversationRepository.cs`: queries preservando `(tenant, usuário, thread, especialista)`.
- `ISM.Infrastructure/Data/Context/IsmDbContext.cs`: mapeamento, índices e migration aditiva.
- `ISM.API/Controllers/Assistant/AssistantController.cs`: contrato HTTP, mantendo controller fino.
- Testes de `AssistantApprovalTests`, `AssistantIntentCoordinatorTests`, `AgentIntentRouterTests`, `SpecialistAgentScenarioTests` e testes de integração de threads.

### Frontend

- `frontend/types/agentExperience.ts` e `frontend/types/assistant.ts`: incluir `finance` e contrato de identidade retornado pelo servidor.
- `frontend/services/modules/assistant/assistantService.ts`: enviar/receber especialista e metadados.
- `frontend/composables/useAssistantUi.ts` e `frontend/composables/useAssistantChat.ts`: `openSpecialistChat`, contexto explícito e proteção contra cross-talk.
- `frontend/components/assistant/AssistantChatDrawer.vue`: renderizar identidade do servidor, não conceder privilégio por fallback visual.
- `frontend/pages/index.vue`, `frontend/pages/financeiro.vue`, `frontend/components/cardapio/CardapioAtual.vue`, `frontend/pages/estoque.vue`, `frontend/pages/fornecedores.vue`.
- `frontend/components/dashboard/AiInsightCard.vue`, `NotificationDrawer.vue`, `AssistantMascot.vue` e `useSystemAlertCenter.ts`: preservar fonte única de alertas.

## Riscos e bloqueios

| Item | Classificação | Tratamento |
|---|---|---|
| Token Telegram exposto na conversa | Segurança | Não utilizar; rotacionar antes de configurar variáveis seguras. Notificações `BLOCKED`. |
| Env de notificação ausente | Bloqueio operacional | Continuar desenvolvimento sem divulgação de segredos; registrar falha separadamente. |
| Disco com 89% de uso | Risco operacional | Monitorar antes de builds Docker; limpar somente cache de build controlado, nunca volumes de dados sem autorização. |
| Serviço Ollama inativo | Bloqueio de benchmark IA | Não impede baseline determinístico; P8 exige iniciar/benchmarkar apenas em ambiente autorizado. |
| 36 vulnerabilidades npm | Pendência preexistente | Não aplicar atualização ampla no P0; avaliar em tarefa própria ou quando afetar dependência modificada. |
| Ausência de `finance` no tipo frontend | Gap funcional | Corrigir em P1 com contrato servidor, sem mascarar como `pricing`. |

## Plano P1 e critérios de entrada

1. Definir enum/string canônica única: `general`, `stock`, `pricing`, `suppliers`, `finance`.
2. Persistir a identidade da thread com migration aditiva e compatibilidade de threads antigas, sem reassignment silencioso.
3. Validar combinação especialista/alerta/entidade no servidor e manter tenant originado da autenticação.
4. Retornar a identidade em thread/conversa e atualizar testes de criação, reload, cross-tenant e histórico.
5. Evoluir composables e CTAs das telas mantendo o centro único de alertas e o launcher geral.
6. Validar backend e frontend após cada bloco; não avançar se autorização, tenant ou persistência falhar.

## Rollback

- Antes de cada alteração, preservar o diff da branch e verificar `git diff --check`.
- Código: revert dos commits isolados da fase, somente após aprovação; nenhum reset destrutivo.
- Banco: migration aditiva e reversão planejada apenas em ambiente de teste; não executar downgrade/reset em dados reais.
- Frontend: manter compatibilidade de `openChat`/`openGeneralChat` durante a migração.
- Infra: usar `docker compose up -d --build` somente após confirmar variáveis/volumes; não executar `down -v`.

## Resultado do gate P0

`PASS` para baseline auditável: branch criada, ambiente inventariado, testes executados, divergências e plano registrados.

`BLOCKED` para notificações Telegram: credenciais seguras ausentes e token exposto exige rotação.

Nenhum código funcional foi alterado. O P1 aguarda aprovação explícita para o primeiro commit desta tarefa, conforme `AGENTS.md` e a skill `efficient-coder`.
