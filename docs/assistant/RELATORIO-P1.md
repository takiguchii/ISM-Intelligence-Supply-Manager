# Relatório P1 — ISM Assist

## Resultado

**P1 concluída e validada.** A camada de execução segura de agentes está disponível no backend, com ferramentas read-only allowlisted, contrato de resultado estruturado, evidências, códigos de decisão, roteamento determinístico e isolamento por tenant/RBAC.

## Entregas

- Registry tipado de ferramentas em `ISM.Application.Agents`.
- Quatro ferramentas read-only registradas:
  - `stock.getProduct`
  - `finance.getRevenueSummary`
  - `pricing.getDishCost`
  - `assistant.getAlertContext`
- `AgentToolResult` com `CanAct`, `NeedsApproval`, `InsufficientData`, `Denied`, `Unreachable` e `Stale`.
- `EvidenceRef` com origem, tenant, instante observado e resumo.
- Contexto de execução derivado da identidade autenticada; `restaurantId` enviado no input é rejeitado.
- Limites de RBAC, timeout por ferramenta e mensagens seguras sem vazamento de exceção.
- Roteador textual determinístico como fallback e validador de intenção estruturada com schema estrito e allowlist.
- Endpoints protegidos:
  - `GET /api/assistant/tools`
  - `POST /api/assistant/tools/execute`

## Validações automatizadas

- `dotnet test backend/ISM.sln --no-restore`: **103 testes aprovados**
  - 64 unitários
  - 38 integração
  - 1 arquitetura
- Cobertura P1 inclui:
  - fixtures isoladas para restaurantes A/B;
  - isolamento cross-tenant sem vazamento de dados;
  - rejeição de `restaurantId` vindo do prompt/input;
  - RBAC e autenticação;
  - evidência e correlation id;
  - schema estruturado, ferramenta desconhecida e fallback determinístico.
- `dotnet build backend/ISM.sln --no-restore`: **sucesso, 0 warnings, 0 errors**.
- `npm run --prefix frontend build`: **sucesso**.

## Validação Docker/API

- `docker compose up -d --build --force-recreate`: concluído.
- MySQL, backend e frontend em execução; backend saudável.
- Sem autenticação em `/api/assistant/tools`: **401**.
- Usuário Manager autenticado:
  - registry retornou as quatro ferramentas esperadas;
  - `stock.getProduct` retornou `CanAct`, dados, evidência `Product` e correlation id;
  - input com `restaurantId` foi rejeitado com `Denied`.

## Observações

Os builds exibem apenas avisos externos já existentes do frontend (Browserslist/chunks grandes) e avisos esperados do EF Core/InMemory durante testes; nenhum deles causou falha.

Nenhum commit foi criado, conforme a regra do repositório.
