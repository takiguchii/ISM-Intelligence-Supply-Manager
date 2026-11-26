# Agente de precificação e CMV

## Responsabilidade periódica

`PricingCmvService` percorre pratos do restaurante, calcula custo pela soma de quantidade de ingrediente vezes custo médio do insumo e calcula CMV sobre o preço de venda. Quando o CMV ultrapassa o limite associado à margem alvo do prato (ou a margem padrão configurada), calcula preço sugerido e persiste alerta de defasagem. Também atualiza o custo armazenado do prato.

Alertas repetidos do mesmo tipo/prato são limitados por janela de 24 horas.

## Ferramentas conversacionais

- `pricing.getDishCost`: preço, custo, margem bruta e alvo para prato específico.
- `pricing.getDishOverview`: visão de até 50 pratos ativos com preço, custo e CMV.
- `pricing.simulateDishPrice`: simula preço para margem alvo informada; não altera cardápio.

A consulta e simulação validam que o prato pertence ao restaurante autenticado.

## Implementação

`PricingCmvService.cs`, `SpecialistAgentTools.cs`, `ReadOnlyAgentTools.cs` e `AgentsBackgroundScheduler.cs`.
