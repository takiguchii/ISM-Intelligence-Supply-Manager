# P1 — Integração integral dos especialistas nas telas

## Resultado esperado pelo usuário

Ao clicar “Conversar” no alerta de precificação da dashboard, abre o chat atual com mascote **e identidade real** de `pricing`, o alerta e prato contextualizados. No cardápio, o botão abre `pricing` mesmo sem alerta. Em fornecedores, abre `suppliers` sem depender de alerta; em financeiro, abre `finance`. Estoque continua funcionando e ganha abertura sem alerta. O ícone global abre `general`.

## Modelo de identidade

Usar identificador canônico e constante (`general`, `stock`, `pricing`, `suppliers`, `finance`). Distinguir `specialistId` de `originAlertId`, `sourceView`, `subjectEntityType` e `subjectEntityId`. Identidade não é derivada apenas de ícone, mensagem inicial ou label.

Backend deve possuir identidade persistente e validada para cada `AssistantThread`, migrando threads antigas para `general` ou especialidade derivável de alerta **após validação no tenant**. Não permitir reassignment silencioso de thread histórica. Restringir combinação especialista/alerta/entidade autorizada. Campo de restaurante vem da autenticação, jamais de payload enviado pelo cliente.

Contrato sugerido (ajustar às convenções reais):

```json
{
  "specialistId": "pricing",
  "originAlertId": 123,
  "sourceView": "dashboard",
  "subjectEntityType": "Dish",
  "subjectEntityId": 42,
  "subject": "Precificação — revisão do prato"
}
```

Campos opcionais, com validação contra o banco e papéis. API retorna `specialistId` no `AssistantThreadResponse`, além do contexto filtrado. Atualizar repositório, EF mapping, migrations, DTOs, service, controllers e tipos TS com testes.

## Frontend

1. Evoluir `useAssistantUi` para expor `openSpecialistChat(specialistId, options?)` e manter compatibilidade com `openGeneralChat` e `openChat(alert)` enquanto migra chamadas.
2. Ajustar `useAssistantChat.ensureReady` para criar/abrir thread com especialista correto e contexto; requisições concorrentes devem ser canceladas/ignoradas por revisão para impedir cross-talk.
3. Em `AssistantChatDrawer.vue`, derivar cabeçalho, nome e `AgentMascot2D` da **identidade do thread fornecida pelo servidor**. Fallback da UI não pode conceder privilégio.
4. Ao mudar especialista, selecionar ou criar thread diferente; histórico e mensagens são isolados por contexto, usuário e tenant. Restaurar foco, responsividade, acessibilidade e loading do componente atual.
5. Na dashboard, associar cada cartão/alerta ao `agentForAlert`; adicionar acesso ao financeiro sem fabricar alertas inexistentes. Sem alerta, abrir especialista sem contexto.
6. No financeiro, alterar `Consultar resumo mensal` para `Conversar com o agente` e abrir `finance`, preservando a página e os gráficos já existentes.
7. No cardápio, incluir componente compacto de `pricing` próximo aos indicadores/itens. Ao abrir a partir de prato, passar ID autorizado do prato; sem prato, abrir chat geral de precificação.
8. Em estoque, preservar o comportamento dos alertas e abrir `stock` quando botão geral da seção ou item sem alerta for acionado (não `general`).
9. Em fornecedores, tornar “Conversar” visível e funcional independentemente de `supplierAlert`.
10. Não adicionar especialista em Pedidos, Funcionários, Integrações e Configurações.

## Competências e limites (política aplicada pelo servidor)

| Especialidade | Pode | Não pode |
|---|---|---|
| `stock` | saldo, consumo, risco, sugestões de reposição; evidências de fornecedores quando imprescindíveis | executar compra, realizar baixa sem aprovação, executar ferramenta financeira |
| `pricing` | custo, CMV, margem, simulação; consultar volume de vendas por prato para análise de rentabilidade | editar preços sem fluxo de aprovação; executar baixas de estoque |
| `suppliers` | comparar preços **históricos/ofertas registradas**, produtos, compras e parceiros existentes | fingir consulta de preço atual na Internet; emitir cotação/compra sem fluxo autorizado |
| `finance` | receitas, despesas, fluxo de caixa, evolução e gráficos com dados reais | criar pagamentos, lançar despesas ou consultar outro tenant via prompt |
| `general` | orientar e, após P4, coordenar leituras dos especialistas | bypass de autorização ou ação sensível direta |

Importante: acesso a dados interdomínio para um relatório é **leitura allowlisted e justificada**, não permissão para executar todas as ferramentas. Ex.: `pricing` pode usar métricas de vendas necessárias ao cálculo de margem, não ferramentas de movimentação financeira.

## Execução de ferramentas e ações

- Preservar as ferramentas existentes descritas em `docs/agents/architecture.md`: `stock.getProduct`, `stock.analyzeReplenishment`, `pricing.getDishCost`, `pricing.simulateDishPrice`, `suppliers.compareOffers`, `finance.getRevenueSummary`, `finance.getPeriodSummary`, `assistant.getAlertContext`.
- Quando a pergunta exigir nova consulta (ex.: pratos mais vendidos), implementar nova ferramenta **tipada e somente leitura**, com query segura, dados estruturados, período e filtros obrigatórios. Não fingir que já existe ferramenta.
- Respostas numéricas devem conter referência a fonte/período e alertar para ausência de dados.
- Mutação exige proposta → aprovação humana → checagem transacional de pré-condições → auditoria, com idempotência e RBAC. Nunca executar SQL arbitrário gerado pelo LLM.
- Se a intenção extrapolar competência, retornar explicação sucinta e opção de encaminhar à especialidade correta, mantendo a trilha de contexto aprovada.

## UX dos gráficos

Adotar payload backend tipado (`metric`, `series`, `labels`, `period`, `source`) e componente frontend controlado; usar biblioteca de gráficos já presente no projeto (`chart.js` / `vue-chartjs`). Não interpretar números em markdown para gerar séries. Sem dados suficientes, exibir estado vazio honesto.

## Critérios de aceite

- Mesmo especialista no CTA, thread persistida, resposta backend, histórico reaberto e UI.
- Cada CTA funciona com e sem alerta quando a tela permite.
- Precificação responde vendas com dados reais se ferramenta existe; caso contrário mostra indisponibilidade **até a ferramenta ser implementada**.
- Permissões cross-tenant e ferramentas não autorizadas são negadas no servidor.
- Todas as mudanças têm testes e commit `fix(assistant): persist specialist identity and wire admin views` (adequar convenção local).
