# Relatório P5 — Automações seguras

## Resultado

**P5 concluída e validada.** O Assistente pode transformar uma solicitação de automação em uma política legível e desativada, exigir confirmação explícita, executar somente análises determinísticas em `dry-run` e registrar cada ciclo.

## Entregas

- `AutomationPolicy` persistida por restaurante com proprietário, especialista, ação permitida, alvo, intervalo, fuso, validade, limite de execuções e versão de concorrência.
- Criação por conversa: pedidos como “crie uma automação diária para verificar meu estoque” geram rascunho `Draft` e `DryRun`.
- Confirmação por conversa ou endpoint separado; sem confirmação a política não é executável.
- Ações permitidas limitadas a análise de alertas de estoque, CMV, ofertas de fornecedores e resumo financeiro.
- Compras, pagamentos, envio externo e alterações de negócio são recusados e não geram execução.
- Scheduler determinístico integrado ao `AgentsBackgroundScheduler`, sem segundo scheduler paralelo.
- `AutomationRun` persistida com status, chave idempotente, horário agendado/real, correlação, motivo e evidência.
- Concorrência otimista no claim da política, dedupe por política + horário agendado e limite de execuções.
- Pausa, revogação e expiração automática; política expirada não é executada.
- Migration `20261117225310_AddAssistantAutomationPolicies` aplicada no MySQL do Docker.
- Correção independente do loading do dashboard preservada: polling só atualiza quando alertas mudam e refresh mantém dados visíveis.

## Validação

- Cenários específicos de automação: **5/5 aprovados**.
  - criação via conversa, confirmação explícita, dry-run, histórico, dedupe, pausa e revogação;
  - recusa de ação insegura e isolamento entre restaurantes.
- Suíte completa: **236 testes aprovados** (191 unitários, 44 integração, 1 arquitetura).
- `dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore`: sucesso; apenas 6 warnings legados de propriedades `Id`.
- `dotnet test backend/ISM.sln`: sucesso.
- `npm run --prefix frontend build`: sucesso; warnings externos conhecidos do ecossistema.
- Docker: backend e MySQL saudáveis; frontend HTTP 200; migration confirmada em `__EFMigrationsHistory`.
- `git diff --check`: aprovado.

## Limites de segurança

Mesmo quando a política é confirmada em modo `Active`, as ações atualmente permitidas continuam sendo somente análises. Nenhuma compra, pagamento, alteração de preço, alteração de estoque ou chamada externa é agendada.

## Controle de versão

- P4: `6705cc6 feat: proteger propostas e aprovacoes do assist`.
- P5 e a correção do loading do dashboard ainda não foram commitadas nesta etapa.
