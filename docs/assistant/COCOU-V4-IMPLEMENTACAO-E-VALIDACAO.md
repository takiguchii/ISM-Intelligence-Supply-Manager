# ISM Assist V4 — Cocou Vivo | Controle de implementação e validação

## P0 — Baseline e inventário inicial (em andamento)

- Branch: `feat/ism-assist-v3-mascot-motion`; HEAD `98b9cf3` (`feat: consolidar automacoes e validacao final do assist`).
- Working tree inicial: dois arquivos não rastreados do solicitante, preservados: `docs/assistant/PROMPT_ISM_COCOU_VIVO_V4.md` e `docs/assistant/Cocou Expression Grid.png`.
- Contribuição/Git: `CONTRIBUTING.md`; commits em português e aprovação explícita prévia exigida por `AGENTS.md`; nenhum commit/push autorizado.
- Referência visual encontrada e aberta: `docs/assistant/Cocou Expression Grid.png` (1774×887). Grade 4×8: corpo squircle mais largo que alto; linhas branco frio, amarelo, roxo e coral/vermelho; olhos pretos pequenos; colunas neutral, esquerda, direita, baixo, cima, triste (olhos caídos), feliz (arcos fechados), preocupado (assimétricos). Não há boca, membros ou acessórios.
- A documentação V3 (especificação mestre, README, relatório de execução e guia de arquitetura) foi consultada. Ela fornece contexto legado, não evidência de atendimento V4. Relatórios P0–P6 da implementação V3 consultados; relatório P6 registra regressão visual completa como não concluída.
- Inventário inicial: componente `AgentMascot2D.vue`, motor `useAgentMascotMotion.ts`, tipos `agentExperience.ts`, launcher/drawer, alert center, cards dashboard, notification drawer, páginas index/dashboard/estoque/financeiro/fornecedores e layout default existem nos caminhos pedidos.
- Baseline visual, matriz integral de dependências/rotas e build/testes V4: NÃO EXECUTADOS ainda.

## Checkpoints P0–P8

| Fase | Estado | Evidência / pendência |
|---|---|---|
| P0 | EM ANDAMENTO | Inventário de invocações e baseline restante |
| P1 | NÃO EXECUTADO | 32 expressões + comparação |
| P2 | NÃO EXECUTADO | Movimento, acessibilidade, teardown |
| P3 | NÃO EXECUTADO | bindings com estados reais |
| P4 | NÃO EXECUTADO | integração das superfícies |
| P5 | NÃO EXECUTADO | responsividade, temas, reduced motion |
| P6 | NÃO EXECUTADO | matriz comportamental e evidências |
| P7 | NÃO EXECUTADO | ciclos de autocorreção |
| P8 | NÃO EXECUTADO | entrega |

## Correção incremental após feedback visual do usuário

- Causa identificada: o pointer listener acompanhava somente o retângulo do mascote; em cards não observava título/texto/área vazia. Agora o listener usa o contêiner `[data-agent-card]`, calcula direção pelo centro do mascote e interpola gaze com amplitude maior.
- Sacadas usam timeout armazenado e cancelável no stop/unmount; a saída do card retorna os olhos ao centro.
- SVG recebeu quatro gradientes de identidade no corpo (branco geral, amarelo financeiro/preço, roxo fornecedores, coral estoque), expressões de olhos diferenciadas e grupo externo de inclinação separado do grupo animado de respiração/squash.
- Launcher mantém um único botão Cocou branco, remove etiqueta visual e botão redundante, respeita safe-area. Eventos existentes e drawer não foram removidos.
- Integração contextual adicionada nos banners/cartões de Estoque, Financeiro e Fornecedores. Financeiro observa somente loading/error/success já fornecidos pela própria página; dados monetários não geram mood. Estoque não fixa severidade crítica.
- Build baseline antes dessas correções: PASSOU (Nuxt 3.21.4; warnings pré-existentes de Browserslist, anotação VueUse e chunk >500 kB).
- `git diff --check`: PASSOU antes da última rodada de edits; rodado novamente junto do build final abaixo.
- Browser visual: NÃO EXECUTADO nesta rodada; Google Chrome existe, mas a sessão exige autenticação e a infraestrutura instalada não inclui Playwright. Capturas autenticadas/light-dark/breakpoint não foram produzidas.
- Testes automatizados de interação: NÃO EXECUTADOS; o package frontend não declara runner de testes.
