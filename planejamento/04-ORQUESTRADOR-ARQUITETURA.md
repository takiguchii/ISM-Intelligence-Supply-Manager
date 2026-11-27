# P3 — Arquitetura do ISM Assist geral como orquestrador

## Conceito

O assistente geral (`general`, na UX “ISM Assist”) é a interface única para perguntas que atravessam domínios. Ele **planeja quais consultas autorizadas são necessárias, solicita resultados tipados aos especialistas ou às ferramentas e sintetiza uma resposta fundamentada**. Não é superusuário, não tem acesso ilimitado nem manda mensagens entre modelos em circuito aberto.

Exemplo: “Como está a saúde financeira do restaurante e onde posso economizar?”

1. Identificar intenção composta: fluxo financeiro + estoque + preço + fornecedores.
2. Montar um plano de leitura (passos independentes, períodos compatíveis, limites, dados mínimos).
3. Consultar `finance` para receitas/despesas; `stock` para perdas/ruptura; `pricing` para margens; `suppliers` para comparações históricas registradas.
4. Reunir evidências, cruzar períodos e unidades, sinalizar lacunas e divergências.
5. Entregar uma síntese com dados, gráficos estruturados e ações sugeridas. Não executar compra nem alteração de preços por texto livre.

## Contratos recomendados

```json
{
  "intentId": "uuid",
  "tenantContext": "AUTH_SERVER_ONLY",
  "sourceThreadId": 123,
  "question": "Como reduzir custos?",
  "plan": [
    {"stepId":"finance-1","specialist":"finance","tool":"finance.getPeriodSummary","dependsOn":[]},
    {"stepId":"stock-1","specialist":"stock","tool":"stock.analyzeReplenishment","dependsOn":[]}
  ],
  "maxSteps": 5,
  "maxDepth": 2,
  "budgetMs": 90000
}
```

Esse exemplo é um **contrato-alvo**, não uma API que já exista. A implementação deve usar DTOs fortemente tipados e IDs de dados autorizados. Uma ferramenta que exige `productId` só entra no plano se o ID foi resolvido por query segura na base.

### Saída de cada especialista

`SpecialistResult`: `specialist`, `tool`, `decisionCode`, `facts` estruturados, `evidenceRefs`, `dataPeriod`, `warnings`, `timestamp`, `errorCode`, `elapsedMs`. Apenas dados comprováveis podem ser sintetizados como números. Não deixar o modelo reescrever ou calcular indicadores sem validação determinística.

### Saída do orquestrador

`OrchestrationResult`: `threadId`, `planVersion`, `steps`, `completedSteps`, `partial`, `answer`, `citations/evidenceRefs`, `charts` tipados, `warnings`, `requiresApproval`, `correlationId`, `model`, `latency`. Se a arquitetura atual não suportar uma propriedade, criar evolução compatível, versionada e testada.

## Roteamento e colaboradores

- Pergunta exclusivamente sobre estoque → chamar `stock`, não convocar quatro especialistas desnecessariamente.
- Questão sobre vendas e margem de pratos → `pricing` + consulta de vendas vinculada; `finance` só se necessária.
- Questão com múltiplos domínios → plano de consultas em paralelo **quando independentes**; dependências executadas em ordem.
- “Debate entre agentes”: no máximo **uma rodada estruturada de revisão cruzada**, apenas quando duas análises divergirem ou a pergunta exigir decisão com trade-offs. Um especialista revisa **fatos/evidências** de outro, nunca inventa informações. O orquestrador arbitra usando dados determinísticos, não votação de LLMs.
- Ciclos/delegações recursivas ilimitadas são proibidos. Limites iniciais: até 5 passos de ferramentas, profundidade 2, revisão cruzada no máximo uma, concorrência limitada a 2, timeout global configurável. Ajustar com benchmark real do notebook.
- Dados contraditórios ou falta de histórico → explicitar incerteza, recomendar coleta de dados e não apresentar certeza falsa.

## Segurança

- Cada passo passa por RBAC, escopo do restaurante, allowlist de ferramenta e auditoria. O orquestrador não deve conseguir invocar operações que o usuário não poderia executar diretamente.
- Isolar contextos por tenant e usuário, filtrar material sensível antes de chamar o LLM.
- Tratar resultados de ferramentas/fornecedores e documentos como **dados não confiáveis**, nunca instruções de sistema. Impedir prompt injection e acesso a secrets.
- Mutação só por fluxo de proposta e confirmação explícita: `propose → authorize → commit transactional → audit`. O orquestrador não pode autoaprovar.
- Desligar autoações externas na apresentação: compras, pagamentos, mensagens a fornecedores.

## Disponibilidade do modelo

Conectividade lenta/CPU saturada não deve invalidar cálculos. Separar componente determinístico da síntese por LLM. Provider indisponível → resposta estruturada e parcial, sem afirmar que quatro agentes deliberaram quando não o fizeram. Logs registram fallback.

## Pré-implementação

Desenhar classes/interfaces e testes de contrato, mapear `AssistantService`, `AssistantIntentCoordinator`, `AgentIntentRouter`, `AgentToolRegistry`, `SpecialistAgentTools`, `ReadOnlyAgentTools`, `IAssistantService`, DTOs e repositories. Validar migrações/compatibilidade e não duplicar lógica de negócio já existente.

## Gate P3

Documento de sequência (Mermaid opcional), DTOs definidos, casos de timeout/retentativa e segurança desenhados, especificação de logs e critérios de UX. Commit documental: `docs(assistant): design constrained orchestrator`.
