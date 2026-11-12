# ISM Assist V3 — Relatório de execução

Data: 2026-11-11
Branch: `feat/ism-assist-v3-mascot-motion`
Base: `8de3969`
Commits nesta branch: nenhum; a política do repositório exige aprovação explícita antes do commit.

## Resultado

A implementação foi realizada sobre a V2 existente, preservando o backend de chat, persistência, alertas, Ollama, dashboard, KPIs, gráficos, navbar, sidebar e tabelas. O robô anterior foi substituído por um único componente SVG inline parametrizado: `frontend/components/assistant/AgentMascot2D.vue`.

Cada agente possui exatamente uma cabeça plana branca/clara, squircle arredondado, dois olhos pretos independentes, badge de acento e sem corpo/props:

- Estoque: vermelho;
- Precificação: prata/branco;
- Fornecedores: azul.

Os três cards são agregados de alertas reais e aparecem na ordem Estoque, Precificação e Fornecedores. Quando não há alerta do agente, o card informa `Sem dados`/nenhuma ocorrência, sem inventar números.

## Etapas e gates

### Etapa 0 — Diagnóstico

**Status:** PASSOU, com bloqueio externo documentado.
**Feito:** especificação mestre lida integralmente; Git, branch, documentação V2, arquitetura, Ollama, API, alertas, chat e baseline auditados.
**Baseline:** backend build PASS; 93 testes .NET PASS; frontend build PASS.
**Limitação:** os três PNGs obrigatórios não existem no repositório/pacote localizado, portanto não foram usados como evidência.

### Etapa 1 — Cabeça SVG 2D

**Status:** PASSOU.
**Arquivos principais:** `AgentMascot2D.vue`, `agentExperience.ts`.
**Evidência:** dashboard capturada com três cards e três mascotes; SVG inline com IDs únicos, sem corpo e sem imagem PNG de personagem.

### Etapa 2 — Motion e expressões

**Status:** PASSOU na validação implementada e manual de navegador.
**Arquivo principal:** `useAgentMascotMotion.ts`.
**Cobertura:** respiração, blink jittered, micro-saccade, gaze por `pointermove` local com `requestAnimationFrame`, retorno ao centro, hover/click squash, pausa por `document.hidden`, IntersectionObserver, reduced-motion e teardown de timers/listeners. Os moods são derivados de dados/estado de chat: `happy`, `worried`, `sad`, `surprised`, `thinking`, `speaking`, `waiting-approval`, `success`, `error` e `offline`.

**Evidência de gaze:** o atributo SVG mudou de `translate(0.00 0.00)` para `translate(3.13 2.14)` após ponteiro extremo, dentro dos limites do contrato.
**Evidência reduced-motion:** classe `ism-mascot-reduced` presente e `animationName: none`.

### Etapa 3 — Alertas reais

**Status:** PASSOU.
**Arquivos principais:** `agentExperience.ts`, `useDashboardMetrics.ts`, `useSystemAlertCenter.ts`, `types/system.ts`.
**Feito:** mapeamento dos cinco tipos legados, incluindo os enums numéricos efetivamente serializados pela API; agregação em três cards; severidade real; contagem de ocorrências/não lidos; dedupe por escopo de restaurante e ID; baseline inicial sem anúncio retroativo; polling pausável.
**Evidência API:** 42 alertas ativos reais no restaurante de desenvolvimento; nenhum número das imagens foi usado como dado de produção.

### Etapa 4 — Dashboard

**Status:** PASSOU.
**Arquivos principais:** `AiInsightCard.vue`, `pages/index.vue`.
**Evidência:** captura `/tmp/ism-dashboard-v3-final.png`: hero, KPIs e gráficos preservados; exatamente três cards; cabeça grande à esquerda; texto e ações `Detalhes`/`Conversar` à direita; launcher discreto.

### Etapa 5 — Detalhes e chat contextual

**Status:** PASSOU na integração existente e smoke browser.
**Feito:** `Detalhes` usa o alerta real; `Conversar` abriu o drawer único com contexto real de `Salmão Fresco em Filé`; thread/contexto é carregado pelos serviços V2; backend permanece tenant-scoped e com fallback determinístico seguro.
**Evidência API:** `/api/assistant/status` respondeu `enabled=true`, `providerReachable=true`, modelo `gemma3:270m`; endpoint de threads retornou 2 threads persistidas.

### Etapa 6 — Sino e popup

**Status:** PASSOU.
**Arquivo principal:** `NotificationDrawer.vue`.
**Feito:** reuso do sino original, badge real, cabeças SVG nos alertas, ações reais, `Escape`, backdrop, leitura/descarte persistidos e sem marcação automática ao fechar.
**Evidência:** `/tmp/ism-notifications-v3-final.png`; abertura confirmou `translate-x-0`, backdrop presente, cabeças visíveis e 42 não lidas.

### Etapa 7 — Estoque

**Status:** PASSOU.
**Arquivo principal:** `pages/estoque.vue`.
**Feito:** banner preservado com cabeça maior preocupada, quantidade/nomes reais, CTA para contexto do Assistente, tabela e ações existentes mantidas.
**Evidência:** `/tmp/ism-stock-v3-final.png`; 2 itens críticos reais e tabela renderizada.

### Etapa 8 — Responsividade e acessibilidade

**Status:** PASSOU nos viewports exercitados; suíte completa de combinações não disponível.
**Evidência:** desktop 1440×1100 e mobile 390×844; `scrollWidth === innerWidth` em ambos, três cards no desktop e sem overflow global no mobile. SVG marcado como decorativo, foco/teclado nos mascotes interativos e Escape no drawer.

### Etapa 9 — QA

**Status:** PASSOU com limitação de runner frontend.
**Comandos:**

- `dotnet build backend/src/ISM.API/ISM.API.csproj` — PASS, 0 warnings/0 errors;
- `dotnet test backend/ISM.sln` — PASS, 93/93 (1 arquitetura, 59 unitários, 33 integração);
- `npm run --prefix frontend build` — PASS (`✨ Build complete!`);
- `git diff --check` — PASS;
- smoke via Chrome CDP — PASS para dashboard, estoque, drawer, Escape, gaze, reduced-motion e chat contextual.

O projeto não possui script de teste frontend e Playwright/Puppeteer não estão instalados; não foi inventada uma suíte ou um PASS de E2E inexistente.

### Etapa 10 — Performance e estabilidade

**Status:** PASSOU na inspeção e smoke disponível.
**Feito:** pointer tracking limitado ao mascote ativo; nenhum HTTP no movimento; RAF/timers/observers são cancelados; aba oculta e reduced-motion interrompem loops; sem biblioteca 3D ou loop `setInterval` de animação. Métrica formal de FPS e profiling de 100–200 remounts não foram coletados.

### Etapa 11 — Documentação e entrega

**Status:** PASSOU sem commit.
**Documentação:** este relatório e o log local ignorado `docs/plannig-local/EXECUTION_LOG.md`.
**Docker:** `docker compose up -d --build` executado; MySQL, backend e frontend ficaram ativos/saudáveis; `databaseConnected=true`. Nenhum volume foi removido e nenhuma migration destrutiva foi aplicada.

## Arquivos V3 principais

- `frontend/components/assistant/AgentMascot2D.vue`
- `frontend/composables/useAgentMascotMotion.ts`
- `frontend/types/agentExperience.ts`
- `frontend/composables/useSystemAlertCenter.ts`
- `frontend/components/dashboard/AiInsightCard.vue`
- `frontend/components/layout/NotificationDrawer.vue`
- `frontend/pages/estoque.vue`
- `frontend/pages/index.vue`

Arquivos V2/backend já presentes na árvore foram preservados e não foram reescritos por conveniência.

## Limitações reais

1. Os três arquivos PNG de referência exigidos não foram encontrados; a comparação foi feita contra o contrato textual e capturas reais do navegador.
2. Não há Playwright/Puppeteer nem runner frontend configurado; a validação de interface foi feita por Chrome headless/CDP e inspeção DOM computada.
3. Não houve geração artificial de um novo alerta para demonstrar o balão de primeira ocorrência; o baseline/dedupe foi implementado sem alterar dados de negócio.
4. Nenhum commit, push, merge ou deploy foi executado.

## Evidências geradas

Capturas locais da execução: `/tmp/ism-dashboard-v3-final.png`, `/tmp/ism-notifications-v3-final.png`, `/tmp/ism-stock-v3-final.png` e `/tmp/ism-dashboard-mobile-v3-final.png`.

## Notificação Audrey

**Status:** enviada e confirmada pelo cliente Telegram da Audrey, usando o chat autorizado configurado no `.env` da Audrey. Token e identificador não foram expostos. O compose da Audrey não estava ativo; o envio foi feito pelo cliente já existente, sem alterar o repositório ou iniciar containers.
