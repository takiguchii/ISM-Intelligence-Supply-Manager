# P6 — Integração, concorrência e carga

## Resultado

**PASS em ambiente MySQL isolado e descartável.** O banco compartilhado não foi alterado.

### Validações executadas

| Cenário | Resultado |
|---|---|
| 20 consultas independentes, 2 etapas cada, fixture determinística | PASS — 1 teste, 40 chamadas, isolamento A/B preservado |
| `dotnet test backend/ISM.sln --no-restore` | PASS após correção — 309 testes (1 arquitetura, 257 unitários, 51 integração) |
| `docker compose config` | PASS |
| Smoke frontend `http://localhost:3000/` | PASS — HTTP 200 |
| Smoke API `http://localhost:8080/swagger/index.html` | PASS — HTTP 200 |
| API protegida sem autenticação | PASS — HTTP 401 |
| Migration completa em MySQL isolado | PASS — última migration `20261124010135_AddAssistantThreadSpecialistIdentity` |
| Login autenticado de gerente no banco isolado | PASS |
| Thread geral + consulta composta financeira/estoque | PASS — resposta determinística com evidência |
| Payload com `restaurantId` no executor de ferramenta | PASS — decisão `Denied` |
| 10 requisições HTTP autenticadas concorrentes | PASS — 10/10 HTTP 200 |

### Baseline do notebook

- Memória: 17 GiB total, 8,6 GiB disponíveis; swap 8 GiB, 4 GiB usada.
- Containers ISM ativos: backend, frontend e MySQL saudáveis/ativos.
- Uso observado: backend 345,9 MiB; frontend 416,5 MiB; MySQL 80,65 MiB.
- Cache Docker liberado com `docker builder prune -f`: 5,408 MiB recuperados.

## Limitações remanescentes

- O MySQL compartilhado permaneceu sem a migration por segurança. A migration foi validada integralmente no banco isolado, que foi encerrado ao final do teste.
- A carga foi executada com Ollama desabilitado, usando o caminho determinístico real; não representa throughput de inferência LLM.
- Não há credenciais seguras de Telegram/Fors configuradas.
