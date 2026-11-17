# ISM Agents — Relatório P0 Baseline

**Data:** 2026-11-15  
**Branch:** `feat/ism-assist-v3-mascot-motion`  
**SHA auditado:** `0ca98e6b6b152b26a4ea291e2a996f56f319183b`  
**Status da prioridade:** `PASSOU`

## Escopo

P0 foi concluída com diagnóstico, correção e validação da cadeia login → frontend → API → MySQL → provider de IA. Nenhum volume foi removido, nenhum reset de banco foi executado e nenhum segredo foi registrado neste relatório.

## Correções realizadas

- O provider de IA passou a resolver `AgentApiEndpoint`, `AgentModel` e `AgentApiKey` do restaurante autenticado, com fallback explícito para `AssistantAiOptions`.
- O status do Assist agora informa o modelo e provider efetivos e verifica se o modelo Ollama selecionado está instalado.
- O transporte passou a tratar Ollama, APIs compatíveis com OpenAI e Anthropic sem registrar chaves.
- Perguntas financeiras são encaminhadas ao `IFinanceService` e respondidas de forma determinística, sem delegação ao LLM quando não há ferramenta financeira estruturada.
- O frontend e o backend passaram a usar `gemma3:270m` como padrão, alinhado ao Compose.
- Foram adicionados testes de regressão para `/api/me/restaurant/agent-config`, modelo efetivo e consulta financeira.

## Ambiente executado

| Item | Resultado |
|---|---|
| Branch/SHA | Confirmados acima |
| `docker compose config --quiet` | PASSOU |
| MySQL 8.4 | Saudável |
| Backend | Saudável em `:8080` |
| Frontend Nuxt | Ativo em `:3000` |
| `/api/system/status` | `databaseConnected=true` |
| `/login` | HTTP 200 |
| Migration mais recente | `20261112000000_AddRestaurantAgentAiConfig` |
| `docker compose run --rm db-migrate` | PASSOU; banco já atualizado |

## Provider de IA

Validações feitas a partir do container do backend:

- conectividade TCP com `host.docker.internal:11434`: PASSOU;
- `GET /api/tags`: PASSOU;
- modelo `gemma3:270m` instalado: PASSOU;
- `/api/chat` direto com resposta sem streaming: PASSOU;
- chamada via `/api/assistant/chat`: modo `llm`, fonte `Ollama local`, sem eco da pergunta;
- configuração customizada por restaurante alterou o modelo efetivo no status e foi restaurada ao padrão.

## Rotas e permissões

| Caso | Resultado |
|---|---:|
| `GET /api/me/restaurant/agent-config` sem token | 401 |
| Mesma rota com gerente | 200 |
| Mesma rota com garçom | 403 |
| `/api/assistant/status` com token inválido | 401 |
| Configuração restaurada sem endpoint/chave persistidos | PASSOU |

## Chat financeiro

`Qual foi meu faturamento este mês?` retornou:

- `mode=deterministic`;
- `source=Dados financeiros do ISM`;
- critério explícito de pedidos pagos, receitas, despesas e contas;
- nenhuma chamada LLM usada para produzir os valores.

## Validação automatizada

- `dotnet restore backend/ISM.sln` — PASSOU;
- `dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore` — PASSOU, 0 erros;
- `dotnet test backend/ISM.sln --no-restore` — PASSOU: 95/95 (1 arquitetura, 59 unitários, 35 integração);
- `npm run --prefix frontend build` — PASSOU;
- `git diff --check` — PASSOU.

O build mantém seis avisos preexistentes de propriedades `Id` que ocultam `BaseEntity.Id`. O frontend mantém avisos preexistentes de Browserslist, anotação do `@vueuse/core` e chunk acima de 500 kB. Nenhum desses avisos bloqueou a execução ou foi mascarado.

## Working tree e migrations

As alterações da task permanecem sem commit, conforme a política do repositório. O snapshot e o arquivo designer da migration de configuração de IA estão presentes; a migration já aplicada no MySQL foi confirmada sem executar reset ou remoção de volume.

Arquivos principais alterados:

- `backend/src/ISM.Application/Interfaces/Assistant/ILlmTextProvider.cs`
- `backend/src/ISM.Application/Services/Assistant/AssistantService.cs`
- `backend/src/ISM.Application/Services/Restaurant/RestaurantAiConfigService.cs`
- `backend/src/ISM.Infrastructure/AI/OllamaTextProvider.cs`
- `backend/src/ISM.Infrastructure/Data/Migrations/IsmDbContextModelSnapshot.cs`
- `backend/src/ISM.Infrastructure/Data/Migrations/20261112000000_AddRestaurantAgentAiConfig.Designer.cs`
- `backend/tests/ISM.IntegrationTests/AssistantApprovalTests.cs`
- `frontend/pages/configuracoes.vue`

## Gate P0

**PASSOU.** Frontend, API, banco e provider estão vivos; a configuração de IA não retorna 500; o status é honesto; o chat simples funciona sem eco; a pergunta financeira usa fonte legítima; permissões básicas foram exercitadas; migrations estão em dia; e as evidências foram registradas sem segredos.

**Próxima prioridade liberada:** P1.
