# ISM Assist V2 — diagnóstico e plano

Data da análise: 2026-11-11
Branch de trabalho: `feat/ai/assistant-ux-v2`
Commit inicial: `8de3969`

## Estado encontrado

A implementação noturna está presente como alterações não commitadas herdadas de
`feat/admin/assistente-ia-ollama`. O build do backend e o build Nuxt passam. A
suíte `dotnet test backend/ISM.sln --no-restore` está bloqueada antes da execução
por `Microsoft.EntityFrameworkCore.Analyzers 8.0.13` ausente no cache NuGet.

## Apresentação a corrigir

- `frontend/pages/index.vue` removeu `AiInsightCard` e `PriceAdjustmentsList` da
  posição original e inseriu `AssistantDashboardHub` antes dos KPIs.
- `frontend/components/assistant/AssistantDashboardHub.vue` é um painel único
  grande, com cabeçalho, abas, prioridades, ações e chat aberto no mesmo bloco.
- `frontend/components/assistant/AssistantMascot.vue` concentra overview, ações e
  chat dentro de um launcher flutuante; está excluído da rota `/` pelo layout.
- `frontend/layouts/default.vue` injeta o mascote globalmente nas demais telas e
  `estoque.vue`, `financeiro.vue` e `fornecedores.vue` disparam eventos globais
  para abri-lo.
- `useDashboardMetrics.ts` deixou de buscar alertas e zerou insights/reajustes,
  eliminando a composição compacta anterior.

## Infraestrutura preservada

- Alertas reais continuam sendo gerados pelos agentes de estoque,
  precificação/CMV e fornecedores e persistidos como `SystemAlert`, com
  deduplicação no backend.
- `SystemAlertsController`, `ISystemAlertService`, `SystemAlertRepository` e as
  preferências de notificação permanecem tenant-scoped; leitura e descarte são
  persistidos.
- Propostas de reajuste, autorização gerencial, auditoria, migration aditiva e
  `OllamaTextProvider` serão reutilizados. O LLM não receberá acesso a SQL nem a
  mutações diretas.

## Lacunas funcionais identificadas

- não existem entidades, rotas ou serviços para threads, mensagens, histórico ou
  ramificação por alerta;
- o chat atual envia somente uma mensagem sem contexto persistido e retorna um
  fallback que não responde deterministicamente a perguntas críticas;
- o detalhe de alerta/insight e a ação “Conversar sobre isso” ainda não existem;
- o sino não oferece descarte nem conversa e o contador depende apenas do drawer
  montado;
- não há store/composable único para polling, deduplicação e balões do mascote;
- a UI usa estados visuais grandes, sem o contrato de drawer lateral recolhido.

## Plano de alterações

1. Restaurar a composição original da dashboard e recolocar insights compactos,
   usando IDs de alertas reais e um detalhe leve.
2. Criar um estado compartilhado do assistente para drawer, contexto, polling
   moderado, deduplicação e balão transitório; integrar o sino ao mesmo estado.
3. Substituir o hub pelo mascote SVG quadrado e pelo drawer de chat independente,
   fechado por padrão e acessível em desktop/mobile.
4. Acrescentar somente os contratos mínimos de thread/mensagem/contexto no
   backend, com tenant e usuário resolvidos no servidor, snapshot read-only e
   fallback determinístico.
5. Integrar cards, detalhes, sino e chat ao vínculo estável do `SystemAlert`, sem
   inventar IDs nem sobrescrever threads existentes.
6. Validar builds, testes, isolamento, estados offline, SSR, responsividade e
   ausência de overflow; registrar limitações de ambiente sem mascará-las.

## Escopo mantido fora desta tarefa

Não será criada a tela de Configurações do Assistente, nem serão alterados
navbar/sidebar globais, motores de estoque/preço/fornecedores ou módulos de
garçom, cozinha e caixa.
