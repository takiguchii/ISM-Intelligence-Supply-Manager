# Relatório P4 — Propostas e execução aprovada

## Resultado

**P4 concluída e validada.** O fluxo de ações do Assistente agora possui expiração, chave de idempotência, versionamento otimista do alvo e auditoria para aprovação, rejeição, expiração e revisão.

## Entregas

- Propostas persistidas com `ExpiresAtUtc` e `IdempotencyKey` por restaurante.
- Expiração automática de propostas pendentes vencidas, com auditoria de sistema.
- Aprovação de preço revalida preço original e `Dish.PriceRevision` antes de alterar o prato.
- `PriceRevision` e `AssistantProposal.Version` usam concorrência otimista; divergência marca a proposta como `Stale` e não altera o valor.
- Reaprovação de proposta decidida, proposta expirada e operação fora do estado pendente não executam nova mutação.
- Rejeição preserva o valor do prato e registra auditoria.
- Isolamento por `RestaurantId` mantido no repositório, nos endpoints e nos testes.
- Migration aditiva `20261117170449_AddAssistantProposalExecutionGuards` aplicada no MySQL do Docker, com retrocompatibilidade para propostas existentes.
- O fluxo de `ReviewAlert` continua separado da mutação de preço; não foram criadas compras, pagamentos ou integrações externas fictícias.

## Validação

- Testes do Assistente: **17 cenários aprovados** após incluir expiração e conflito de revisão.
- Suíte completa: **231 testes aprovados** (189 unitários, 41 integração, 1 arquitetura).
- `dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore`: sucesso.
- `dotnet test backend/ISM.sln`: sucesso.
- `npm run --prefix frontend build`: sucesso; apenas avisos externos conhecidos de Browserslist, Rollup e chunks grandes.
- `git diff --check`: aprovado.
- Docker: imagens backend/frontend reconstruídas; MySQL e backend saudáveis; frontend HTTP 200.
- Migration verificada em `__EFMigrationsHistory` no MySQL em execução.

## Limites conscientes

O domínio atual não possui infraestrutura real de compras, cotações externas ou pagamentos. Por isso a P4 entrega propostas auditáveis e a alteração de preço já existente, sem simular pedido enviado ou operação financeira.

## Controle de versão

- P0–P2: `85e06dc feat: implementar especialistas funcionais do assist`.
- P3: `e8a8d24 feat: coordenar conversa contextual do assist`.
- Alterações desta P4 ainda não foram commitadas nesta etapa.
