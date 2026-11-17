# Relatório P2 — Especialistas funcionais do ISM Assist

## Resultado

**P2 concluída e validada.** Os especialistas funcionais foram implementados como ferramentas read-only determinísticas, com schemas de entrada, evidências, limites de tenant/RBAC e recusa explícita quando os dados não são suficientes.

## Especialistas entregues

- **Estoque — `stock.analyzeReplenishment`**
  - calcula consumo negativo dos últimos 30 dias;
  - calcula consumo diário e dias restantes;
  - sugere quantidade apenas como simulação;
  - não executa compra;
  - retorna `InsufficientData` quando não há consumo confiável ou quando faltam pedidos de reposição abertos.
- **Precificação — `pricing.simulateDishPrice`**
  - recalcula custo pela ficha técnica e custo médio dos ingredientes;
  - calcula CMV atual, margem alvo, preço sugerido e variação;
  - valida margem entre 0% e 100%;
  - não altera o prato.
- **Fornecedores — `suppliers.compareOffers`**
  - compara histórico de preços por produto e unidade compatível;
  - calcula média ponderada por quantidade;
  - elimina ofertas antigas e incompatíveis;
  - não envia cotação.
- **Financeiro — `finance.getPeriodSummary`**
  - consulta pedidos pagos e lançamentos liquidados;
  - valida período UTC de até 366 dias;
  - calcula receita, outras entradas, despesas e fluxo líquido;
  - retorna `InsufficientData` quando não há fonte efetiva no período.

## Segurança e contratos

- Todas as ferramentas são `Read` e `RequiresApproval = false`.
- Nenhuma ferramenta aceita `restaurantId` na entrada.
- A validação permanece ativa mesmo em chamada direta ao executor, além da proteção do registry.
- JSON inválido, tipos incorretos, arrays, `null`, IDs inválidos e campos ausentes retornam decisão segura sem exceção.
- Os quatro novos descriptors expõem schema de entrada na API.
- Evidências sempre carregam o `RestaurantId` do contexto autenticado.

## Gate P2

- Matriz P2: **100/100 cenários aprovados**.
  - 40 cenários AGIR: 10 por especialista.
  - 60 cenários NÃO AGIR: entradas ausentes, inválidas, fora do tenant, dados ausentes, período inválido, unidade incompatível e dados sem validade.
- Suíte completa: **203 testes aprovados**.
  - 164 unitários;
  - 38 integração;
  - 1 arquitetura.
- `dotnet build backend/ISM.sln --no-restore`: sucesso, 0 warnings, 0 errors.
- `npm run --prefix frontend build`: sucesso; avisos externos de Browserslist/chunks permanecem sem falha.
- `git diff --check`: aprovado.

## Docker e smoke real

- `docker compose up -d --build --force-recreate`: concluído.
- MySQL e backend saudáveis; frontend em execução.
- Registry autenticado retornou 8 ferramentas, incluindo os 4 especialistas P2.
- Execução real de estoque sem consumo retornou `InsufficientData` sem inventar recomendação.
- Tentativa real com `restaurantId` injetado retornou `Denied`.

## Limitação documentada

O modelo atual não possui uma entidade/repositório de pedidos de reposição abertos. O especialista de Estoque não finge que essa informação existe: quando ela é necessária para uma sugestão segura, retorna `InsufficientData`. A próxima etapa pode criar esse fluxo somente se houver domínio real para pedido, aprovação e auditoria.

Nenhum commit foi criado, conforme as regras do repositório.
