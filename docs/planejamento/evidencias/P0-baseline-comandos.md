# Evidências P0 — comandos executados

Este arquivo contém somente resultados operacionais não sensíveis. Nenhum token, chat ID, dado de cliente ou conteúdo de banco real foi registrado.

## Git

- Branch criada: `feat/assistant-orquestracao-p0`
- Base: `develop` em `351f1c8`
- Estado inicial e final antes do relatório: `?? planejamento/`

## Ferramentas

- `dotnet --info`: SDK 8.0.132 / runtime 8.0.32 / Fedora 43
- `node --version`: v22.22.2
- `npm --version`: 10.9.7
- `docker compose version`: 5.6.0
- `ollama --version`: 0.32.9
- `docker compose config --quiet`: exit 0

## Testes

- `dotnet test backend/ISM.sln --no-restore`: PASS — 263/263
- `npm ci --prefix frontend`: PASS — instalação concluída; npm audit reportou 36 vulnerabilidades preexistentes
- `npm run --prefix frontend build`: PASS — Nuxt/Nitro produzido
- `npm run --prefix frontend test:waiter`: PASS — 7/7
- `npm run --prefix frontend test:system-alerts`: PASS — 2/2
- `npm run --prefix frontend test:kitchen`: PASS — 7/7
- `npm run --prefix frontend test:confirmation`: PASS — 4/4
- `npm run --prefix frontend test:cash`: PASS — 12/12

## Infraestrutura observada

- Ollama instalado, serviço de usuário inativo; nenhum modelo foi baixado ou iniciado.
- 6 containers já estavam ativos antes de qualquer ação Docker; nenhum foi reiniciado.
- Docker reportou 1.542 GB de build cache e 8.029 GB de volumes reclaimable; volumes não foram removidos.
