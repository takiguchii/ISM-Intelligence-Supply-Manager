# Agente de estoque

## Responsabilidade periódica

`StockAgent` recalcula consumo médio diário com base nas movimentações negativas elegíveis dos últimos 30 dias. Se não houver consumo registrado, usa `quantidade mínima / 7` como estimativa; sem consumo nem mínimo configurado, o consumo fica em zero.

Gera alertas quando o saldo projetado alcança ruptura em até 48 horas ou fica abaixo do mínimo. A chave de deduplicação é restaurante, tipo de alerta e produto, com janela de 24 horas. O agente atualiza `MovingAverageConsumption` e `LastConsumptionRecalculatedAtUtc`.

## Ferramentas conversacionais

- `stock.getProduct`: saldo, mínimo e risco de um produto.
- `stock.getOverview`: visão dos produtos e itens críticos, limitada a 50 itens.
- `stock.analyzeReplenishment`: consumo negativo dos últimos 30 dias e sugestão de reposição; não compra nem altera estoque.

Todas as consultas validam o restaurante do produto contra o tenant autenticado.

## Execução periódica

Chamado pelo `AgentsBackgroundScheduler` a cada cinco minutos, dentro de escopo de serviço e contexto de tenant por restaurante. O agente periódico também pode ser chamado com um restaurante específico.

## Implementação

`StockAgent.cs`, `SpecialistAgentTools.cs`, `ReadOnlyAgentTools.cs` e `AgentsBackgroundScheduler.cs`.
