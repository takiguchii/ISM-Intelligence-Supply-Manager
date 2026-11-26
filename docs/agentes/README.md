# Agentes do ISM

Documentação oficial do comportamento implementado no código. As consultas usam o restaurante da identidade autenticada; `restaurantId` nunca é argumento fornecido pelo modelo ou pelo cliente.

## Componentes

| Documento | Escopo |
| --- | --- |
| [Assistente geral](assistente-geral.md) | Conversas, roteamento de intenções e resposta factual |
| [Agente de estoque](estoque.md) | Consumo, ruptura e estoque mínimo |
| [Agente de fornecedores](fornecedores.md) | Variações de preço e alternativas de compra |
| [Agente de precificação](precificacao.md) | Custo, CMV, margem e alertas |
| [Agente financeiro](financeiro.md) | Consultas de receita, pedidos, despesas e vendas |
| [Orquestrador](orquestrador.md) | Planejamento e coordenação de consultas entre especialistas |
| [Ferramentas](ferramentas.md) | Catálogo, segurança e ciclo de execução |

## Fluxo resumido

```text
Usuário autenticado
        |
        v
AssistantController -> AssistantService -> roteador determinístico
                                  |                  |
                                  |                  +--> ferramenta especializada (leitura)
                                  |                           |
                                  +--> orquestrador (2+ domínios) --> registry allowlist --> ferramentas
                                  |
                                  +--> LLM com contexto factual atual (quando necessário)

Agentes periódicos: scheduler a cada 5 min -> escopo por restaurante -> estoque,
fornecedores, precificação e automações em dry-run -> alertas/auditoria.
```

## Limites operacionais

- As ferramentas conversacionais registradas são somente leitura. Simulações e propostas não alteram preços, compras, pagamentos ou estoque.
- A IA pode sintetizar fatos, mas os dados operacionais vêm dos repositórios e dos serviços do ISM.
- As automações agendadas são simuladas; não executam ações externas nem mutações de negócio.
- Falha ou ausência de dados deve aparecer como resposta parcial/insuficiente, sem fabricar números.

## Código-fonte

- Registro e DI: `backend/src/ISM.API/Extensions/ServiceCollectionExtensions.cs`.
- Contratos: `backend/src/ISM.Application/Agents/`.
- Ferramentas: `backend/src/ISM.Application/Services/Agents/`.
- Chat e orquestração: `backend/src/ISM.Application/Services/Assistant/`.
- Execução periódica: `backend/src/ISM.API/HostedServices/AgentsBackgroundScheduler.cs`.
