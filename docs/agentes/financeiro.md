# Agente financeiro

## Responsabilidade

O especialista `finance` concentra consultas determinísticas de receitas, pedidos, despesas, fluxo de caixa e desempenho de pratos. As fontes são o serviço financeiro e os pedidos pagos do restaurante autenticado.

## Ferramentas

- `finance.getRevenueSummary`: resumo financeiro do mês corrente.
- `finance.getPeriodSummary`: receita, despesas e fluxo de caixa entre datas UTC fornecidas (período máximo de 366 dias).
- `finance.forecastRevenue`: estimativa informativa para sete dias, baseada em vendas pagas recentes; não é garantia.
- `sales.getDishInsights`: ranking de pratos, lucro realizado ou estimativa de demanda com base em pedidos pagos.
- `orders.getRecent`: até dez pedidos recentes; vendas somadas apenas para pedidos pagos.

Os resultados não constituem previsão garantida e a ferramenta de histórico de pedidos não representa vendas não pagas como receita realizada.

## Implementação

`ReadOnlyAgentTools.cs`, `SpecialistAgentTools.cs`, `AgentIntentRouter.cs` e `AssistantService.cs`.
