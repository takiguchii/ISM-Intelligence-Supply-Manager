# P6 — Integração, concorrência e carga

## Resultado

**PASS parcial / BLOCKED de infraestrutura externa.**

### Validações executadas

| Cenário | Resultado |
|---|---|
| 20 consultas independentes, 2 etapas cada, fixture determinística | PASS — 1 teste, 40 chamadas, isolamento A/B preservado |
| `dotnet test backend/ISM.sln --no-restore` | PASS após correção — 308 testes (1 arquitetura, 256 unitários, 51 integração) |
| `docker compose config` | PASS |
| Smoke frontend `http://localhost:3000/` | PASS — HTTP 200 |
| Smoke API `http://localhost:8080/swagger/index.html` | PASS — HTTP 200 |
| API protegida sem autenticação | PASS — HTTP 401 |

### Baseline do notebook

- Memória: 17 GiB total, 8,6 GiB disponíveis; swap 8 GiB, 4 GiB usada.
- Containers ISM ativos: backend, frontend e MySQL saudáveis/ativos.
- Uso observado: backend 345,9 MiB; frontend 416,5 MiB; MySQL 80,65 MiB.
- Cache Docker liberado com `docker builder prune -f`: 5,408 MiB recuperados.

## Bloqueio real

Não executei fluxo destrutivo ou migration no MySQL compartilhado. O banco isolado/descartável exigido pelo P6 não está configurado no ambiente atual; por isso concorrência HTTP autenticada, atualização real de dataset e aplicação da migration permanecem **BLOCKED**, sem serem declaradas como PASS.

Também não há credenciais seguras de notificação nem provider Ollama ativo configurado para uma carga LLM real. O fallback determinístico e os testes com fixture permanecem disponíveis para a demonstração.
