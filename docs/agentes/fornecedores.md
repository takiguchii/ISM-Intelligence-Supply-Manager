# Agente de fornecedores

## Responsabilidade periódica

`SupplierAgent` analisa histórico de compras dos últimos 60 dias, agrupado por nome normalizado de produto e fornecedor.

- Aumento: compara preço mais recente dos últimos sete dias com a média ponderada por quantidade dos últimos 30 dias. Emite alerta quando há ao menos duas compras na janela de 30 dias e o preço recente é pelo menos 15% maior; aumento de 25% ou mais é classificado como perigo.
- Alternativa: compara preços recentes entre fornecedores e sugere alternativa quando um fornecedor está pelo menos 10% abaixo de outro.
- Alertas são deduplicados por chave composta de restaurante, produto e fornecedores, com janela de 48 horas.

## Ferramenta conversacional

`suppliers.compareOffers` compara registros históricos compatíveis por produto e unidade, com janela opcional de 1 a 365 dias. Não consulta cotação ao vivo; a resposta explicita essa limitação.

O roteador exige produto e unidade identificáveis. Sem esses dados não deve criar consulta com produto presumido.

## Implementação

`SupplierAgent.cs`, `SpecialistAgentTools.cs`, `AgentIntentRouter.cs` e `AgentsBackgroundScheduler.cs`.
