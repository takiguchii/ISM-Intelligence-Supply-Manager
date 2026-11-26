# Ferramentas dos agentes

Todas as ferramentas abaixo são registradas como `IAgentTool` e executadas pelo `AgentToolRegistry`. A lista de runtime também pode ser consultada pela rota de ferramentas do `AssistantController`.

| Nome | Especialista | Finalidade |
| --- | --- | --- |
| `stock.getProduct` | stock | Saldo e risco de um produto |
| `stock.getOverview` | stock | Visão geral e itens críticos do estoque (até 50) |
| `stock.analyzeReplenishment` | stock | Consumo recente e sugestão sem compra |
| `pricing.getDishCost` | pricing | Preço, custo e margem do prato |
| `pricing.getDishOverview` | pricing | Visão de pratos ativos (até 50) |
| `pricing.simulateDishPrice` | pricing | Simulação de preço sem alteração |
| `suppliers.compareOffers` | suppliers | Comparação de histórico de compra por produto/unidade |
| `finance.getRevenueSummary` | finance | Resumo financeiro do mês corrente |
| `finance.getPeriodSummary` | finance | Resumo para intervalo de datas |
| `finance.forecastRevenue` | finance | Estimativa informativa de receita |
| `sales.getDishInsights` | finance | Desempenho e demanda por prato |
| `orders.getRecent` | finance | Pedidos recentes e vendas pagas |
| `orders.getLatest` | assistant | Pedido mais recente |
| `assistant.getAlertContext` | assistant | Fatos persistidos de um alerta |

## Validações do registry

1. Requer papel gerencial e `RestaurantId` autenticado.
2. Exige ferramenta registrada e especialista correspondente.
3. Exige entrada JSON objeto e recusa `restaurantId` enviado pelo cliente/modelo.
4. Valida papel configurado no descritor.
5. Aplica timeout do descritor (limitado a 100–30.000 ms), correlation ID e logging de exceção.
6. Devolve um código explícito: `CanAct`, `NeedsApproval`, `InsufficientData`, `Denied`, `Unreachable` ou `Stale`.

As operações conversacionais registradas são `Read`; o registry não expõe ferramenta de compra, pagamento, alteração de preço ou mutação de estoque. Uma simulação ou recomendação não deve ser descrita como execução.

## Modelos e contratos

Descritores incluem nome, especialista, versão, finalidade, operação, papéis, limite de resultados, prazo, aprovação, risco e esquema de entrada. Resultados carregam dados, evidências com tenant e temporalidade, mensagem, correlação e duração. Ver `backend/src/ISM.Application/Agents/AgentContracts.cs`.
