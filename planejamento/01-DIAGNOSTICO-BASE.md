# P0 — Diagnóstico obrigatório e baseline

## Componentes encontrados no snapshot

- Frontend: Nuxt 3 / Vue 3. `frontend/composables/useAssistantUi.ts` oferece `openChat(alert, draft)` e `openGeneralChat(draft)`; não aceita agente explícito.
- `frontend/composables/useAssistantChat.ts` inicializa por `originAlertId`; criação de thread não passa identidade especializada.
- `frontend/types/agentExperience.ts` define `AgentKind` como `stock | pricing | suppliers | general` (não inclui financeiro).
- `frontend/components/assistant/AssistantChatDrawer.vue` determina `chatAgent` pelo contexto do alerta e exibe o mascote; confirmação do papel **não é o mesmo que autorização no backend**.
- `frontend/pages/financeiro.vue` chama `openGeneralChat(...)` e o botão existente mostra “Consultar resumo mensal”.
- `frontend/pages/fornecedores.vue` apresenta abertura de conversa condicionada ao alerta.
- `frontend/pages/index.vue` roteia abertura pelo alerta, caindo no geral quando ausente.
- `backend/src/ISM.Application/DTOs/Assistant/AssistantDtos.cs`: `AssistantCreateThreadRequest` contém `OriginAlertId`, `ParentThreadId`, `Subject`; não contém especialista.
- `backend/src/ISM.Domain/Modules/Assistant/Entities/AssistantThread.cs`: possui `RestaurantId`, `UserId`, `OriginType`, `OriginAlertId`, `ContextSnapshotJson`, mas não tem coluna dedicada a especialista.
- `backend/src/ISM.Application/Services/Agents/AgentIntentRouter.cs`: mapeia ferramentas a especialidade, porém o roteamento de texto é parcial e não equivale ao escopo fixo da thread.
- `docs/agents/architecture.md`: fontes determinísticas e regras de autorização são essenciais; preservar sua filosofia.

## Verificações antes da primeira edição

1. `git status --short`, `git branch --show-current`, `git log -1 --oneline` e identificar código não commitado. Não sobrescrever trabalho de outras pessoas.
2. Conferir `docs/ISM_ASSIST_V3_ESPECIFICACAO_MESTRE.md`, `docs/agents/architecture.md`, `DEV_GUIDELINES.md`, regras de Git e migrations existentes.
3. Comparar caminhos e contratos acima com a branch atual; guardar divergências no relatório.
4. Mapear chamadas do frontend a controllers e services, histórico de chat, autorização, tenant scope, fallback de provider, componentes visuais e testes.
5. Rodar `dotnet --info`, `node --version`, `npm --version`, `docker compose version`, `ollama --version`; identificar o que está instalado sem instalar dependências globalmente às cegas.
6. Preparar ambiente de teste isolado. Não usar dados de restaurantes reais nem `docker compose down -v`.
7. Rodar baseline: `dotnet test backend/ISM.sln`, `cd frontend && npm ci && npm run build`; scripts de testes específicos do `frontend/package.json`; testes em contêiner quando seguros. Capturar sucesso, falhas e bloqueios.

## Riscos iniciais a validar, sem presumir bugs confirmados em produção

- Threads não fixam o tipo de especialista no contrato persistido.
- Troca de mascote sem troca de autorização/backend.
- Conversa aberta por alerta com contexto incompleto ou desatualizado.
- Respostas de precificação quando o pedido não traz ID do prato.
- Cargas em CPU de notebook com timeout muito baixo (`ASSISTANT_AI_TIMEOUT_SECONDS=20` no `.env.example`).
- Multitenancy, autorização e execução de propostas de escrita.
- Testes existentes de roteamento e especialistas: `backend/tests/ISM.UnitTests/AgentIntentRouterTests.cs`, `SpecialistAgentScenarioTests.cs`, `AssistantIntentCoordinatorTests.cs`.

## Saída da fase

Criar relatório `relatorios/P0-baseline.md` com inventário de mudanças propostas, interfaces que serão evoluídas, versão do ambiente, tempo/resultado de testes, riscos e plano de rollback. Não mudar dados reais.
