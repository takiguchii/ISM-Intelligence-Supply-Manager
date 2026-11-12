# ISM Assist V2 — relatório de entrega

## Escopo entregue

Refatoração incremental da experiência administrativa do ISM Assist, preservando a infraestrutura de agentes, alertas, Ollama e ações existentes. A dashboard voltou a usar sua composição original: KPIs e gráficos permanecem no fluxo principal, os insights passaram a cartões compactos e o chat fica fechado por padrão em um drawer lateral.

Branch de trabalho: `feat/ai/assistant-ux-v2`
Commit-base: `8de3969892c153cbcfecee38f385a4609995b327`
Commits criados nesta tarefa: nenhum. Push, merge e deploy: não executados.

## Principais mudanças

### Dashboard e notificações

- Removido da composição da dashboard o `AssistantDashboardHub`, que ocupava a área central.
- `AiInsightCard.vue` foi reduzido a cartão de notificação com especialista, severidade textual, dado factual, não lido, horário, detalhes e conversa contextual.
- `AgentInsightDetails.vue` exibe origem, gravidade, data, entidade e fatos autorizados sem navegação desnecessária.
- `useSystemAlertCenter.ts` centraliza alertas, preferências, contador, leitura, descarte, polling de 45 segundos, pausa por visibilidade e deduplicação de balões.
- `NotificationDrawer.vue` e `AppNavbar.vue` passaram a usar o mesmo estado de alertas, sem segunda central paralela.
- Administrador global só vê os alertas do restaurante atualmente selecionado na dashboard; as APIs continuam resolvendo o tenant no servidor.

### Mascote e chat

- `AssistantMascot.vue` implementa robô SVG próprio, pequeno e não invasivo, com estados idle, thinking, speaking, warning e offline.
- Animações usam transform/opacity, têm piscada/respiração discretas e são desativadas com `prefers-reduced-motion`.
- Balão transitório aparece apenas para ocorrências novas relevantes, agrupa simultâneos e não marca alerta como lido ao ser fechado.
- `AssistantChatDrawer.vue` usa overlay e drawer independente, é fechado inicialmente, funciona em mobile, fecha por Escape e não renderiza conteúdo por `v-html`.
- `useAssistantChat.ts` mantém threads, mensagens, loading, erro, retry, histórico e nova conversa usando os serviços reais do backend.

### Backend e persistência

- Foram adicionadas as entidades `AssistantThread` e `AssistantMessage`, com usuário, restaurante, thread-pai, origem do alerta, entidade relacionada, snapshot factual e timestamps.
- Migração aditiva `20261111113422_AddAssistantConversations` criada e aplicada no MySQL local sem apagar dados.
- Novas operações autenticadas:
  - `GET /api/assistant/threads`
  - `POST /api/assistant/threads`
  - `GET /api/assistant/threads/{id}`
  - `GET /api/assistant/alerts/{id}/context`
  - `POST /api/assistant/threads/{id}/messages`
- O servidor valida usuário, papel, restaurante e alerta de origem. ID de alerta de outro restaurante resulta em `404` e não cria thread.
- O contexto é montado no backend a partir de dados tipados de estoque/alerta; o cliente não escolhe livremente o tenant.
- Perguntas críticas de estoque usam respostas determinísticas com quantidade e mínimo reais. Perguntas abertas usam Ollama somente para redação, com contexto limitado e fallback explícito quando o provedor está indisponível.
- O chat não executa SQL, comandos, alterações de estoque/preço/fornecedor nem trata texto do modelo como autorização.
- A infraestrutura anterior de propostas de preço, aprovação, rejeição, auditoria e validação de ações foi preservada.

## Verificações executadas

- `dotnet build backend/src/ISM.API/ISM.API.csproj` — passou.
- `dotnet test backend/ISM.sln` — passou: 1 arquitetura, 59 unitários e 33 integração; 93 testes no total.
- Testes de integração adicionados para criação contextual, números reais, retomada após reload e bloqueio de alerta de outro restaurante.
- `npm run --prefix frontend build` — passou; build client e SSR/Nitro concluídos.
- Smoke HTTP de `GET /api/system/status` — passou com `databaseConnected: true`.
- Smoke do servidor Nuxt em `/login` com Chrome headless — HTML servido corretamente.
- Ollama local verificado com o modelo configurado `gemma3:270m`; health check e pergunta simples responderam.
- `db-migrate` aplicado em ambiente local: `20261111113422_AddAssistantConversations` concluída.
- `git diff --check` — passou.
- Busca final não encontrou referências ao megacomponente removido, composable antigo redundante ou `v-html` no chat/alertas.

## Avisos e limites reais

- O repositório não possui scripts/configuração de Vitest ou Playwright nem fixture de login administrativo; por isso não foi possível produzir E2E autenticado ou screenshot comparativo da dashboard sem inventar credenciais. O frontend foi validado por build, smoke HTTP e inspeção do código executável; o backend contextual foi validado por integração.
- Permanecem avisos preexistentes do projeto: propriedades `Id` que ocultam `BaseEntity.Id`, base Browserslist desatualizada, anotações Rollup de `@vueuse/core` e chunk frontend acima de 500 kB.
- O histórico mostra logs de avisos de testes existentes do EF Core e endpoints de pedidos; a suíte terminou sem falhas.
- O modelo local continua limitado para raciocínio complexo. O sistema responde deterministicamente aos intents críticos cobertos e informa falta de dados quando não há evidência suficiente.

## Próxima tarefa

Não foi criada nem alterada uma tela nova de Configurações do Assistente. Ficam para uma tarefa posterior: modelo/provedor, preferências do mascote, política de notificações, memória, temperatura e timeout.
