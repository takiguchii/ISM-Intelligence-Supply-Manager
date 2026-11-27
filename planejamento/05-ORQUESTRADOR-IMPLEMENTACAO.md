# P4 — Construção do orquestrador geral

## Ordem técnica

1. Criar camada de orquestração na Application usando interfaces existentes, sem importar Infrastructure para Domain.
2. Implementar detecção/validação de intenções e planejador fortemente tipado, com fallback determinístico para intents óbvias. O LLM pode **propor** um plano JSON restrito; servidor valida schema, ferramenta, permissão, argumentos e limite de custo.
3. Implementar `ExecutePlanAsync` com correlation ID, cancelamento, budget global, timeout por ferramenta, concorrência controlada, logs e isolamento de falhas. Nunca disparar uma chamada para ferramenta com ID inferido de texto não verificado.
4. Implementar síntese com evidências estruturadas; números e rankings vêm das respostas das ferramentas. Reter `partial` e avisos quando um passo falha.
5. Acrescentar revisão cruzada opcional por especialista **uma rodada**, somente quando agregue valor concreto, com registros `claim`, `evidence`, `contradiction`, `resolution`.
6. Integrar ao `AssistantService` para threads `general` (do P1); preservar fluxos diretos para `stock`, `pricing`, `suppliers`, `finance`.
7. Persistir execução e observabilidade na arquitetura atual (não necessariamente criar tabela para cada detalhe; priorizar audit log já existente). Evitar armazenar prompt contendo segredos.
8. Atualizar UX do chat: status “Consultando financeiro/estoque…”, origens dos dados, aviso de síntese parcial, gráficos e opção de abrir conversa especializada contextualizada. Manter visual e mobile atuais.

## Exemplo de conversa esperada

Usuário: “Minhas vendas aumentaram, mas minha margem caiu. Por quê?”

Orquestrador:
- Obter períodos comparáveis de faturamento e volume de pedidos.
- Obter custos/CMV dos pratos relevantes e histórico de preços/fornecedores.
- Explicar causas comprovadas, separar hipótese de dado confirmado, indicar próximos passos.
- Gráfico comparando vendas e CMV só se backend fornecer dados estruturados completos.

**Resposta inaceitável:** “Seu CMV subiu 17%” sem dataset ou tool result. **Resposta aceitável quando falta informação:** “Ainda não há histórico suficiente para atribuir a queda de margem a uma causa específica.”

## Assinaturas e contratos

Produzir contrato equivalente a:

```
IOrchestrationPlanner.PlanAsync(AssistantQueryContext, CancellationToken)
IOrchestrationExecutor.ExecuteAsync(ValidatedPlan, CancellationToken)
IOrchestrationSynthesizer.SummarizeAsync(ExecutionEvidence, CancellationToken)
```

Não impor esses nomes se conflitam com as convenções, mas manter separação de responsabilidades. Cada executor da etapa recebe **contexto do servidor** (usuário/tenant/papel, correlation ID), nunca parâmetros de autorização do LLM.

## Custos e desempenho

- Evitar quatro chamadas de LLM quando uma consulta determinística basta.
- Se o modelo local está no mesmo notebook do ISM, limitar concorrência e contexto; medir tempo fim a fim.
- Disponibilizar modo `ORCHESTRATOR_CROSS_REVIEW=false` para a apresentação e habilitar somente após validação, se CPU não comportar.
- Registrar tempo por passo e cache apenas para leitura tenant-scoped com TTL curto e invalidação definida.

## Validações antes do commit

Unit: planner escolhe domínio correto, plano inválido é rejeitado, custo máximo é respeitado, loop não ocorre. Integration: servidor chama ferramentas corretas com scopes autorizados, resultados com evidências, partial failure. UI: chat central abre corretamente, não confunde especialistas, gráfico tem data source. Security: negar tenant B, papel não autorizado, tool write sem consentimento.

Gerar `relatorios/P4-orquestrador-implementacao.md`, registrar feature flags e executar commit `feat(assistant): orchestrate specialist analyses safely`.
