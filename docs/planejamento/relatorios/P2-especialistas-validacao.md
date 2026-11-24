# P2 — Validação dos especialistas

## Resultado

**PASS** para os especialistas de estoque, precificação, fornecedores e financeiro.

O conjunto determinístico `SpecialistAgentScenarioTests` exercita 10 cenários positivos e 15 cenários de recusa por especialista (incluindo entradas ausentes, tipos inválidos, IDs inexistentes, limites inválidos e tentativa de tenant cruzado). Cada execução verifica o código de decisão e confirma que nenhuma evidência pertence a outro restaurante.

## Evidências

| Comando | Resultado |
|---|---|
| `dotnet test backend/tests/ISM.UnitTests/ISM.UnitTests.csproj --no-restore --filter FullyQualifiedName~SpecialistAgentScenarioTests` | PASS — 100 casos |
| `dotnet test backend/ISM.sln --no-restore` | PASS — 265 casos |
| `git diff --check` | PASS |

Os especialistas usam ferramentas tipadas e repositórios injetados; não há autorização baseada no texto gerado pelo modelo. A validação de autorização e isolamento de restaurante permanece no backend e foi reforçada pelos testes P1 de identidade da thread.

## Pendências externas

- `notifications=BLOCKED`: `FORS_BOT_TOKEN`/`FORS_CHAT_ID` não foram fornecidos no ambiente seguro.
- Não foi aplicada migration no MySQL compartilhado; a aplicação será validada somente em banco de teste isolado.
