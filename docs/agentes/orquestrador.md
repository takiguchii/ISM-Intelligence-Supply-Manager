# Orquestrador

## Responsabilidade

`ConstrainedOrchestrationService` planeja consultas determinísticas quando a pergunta combina domínios, executa as ferramentas e resume resultados, evidências e falhas parciais. Ele não usa o LLM para escolher ou executar ferramentas.

## Planejamento

- Detecta consultas financeiras, produto identificado por ID, prato identificado por ID e intenção de fornecedor com produto/unidade explícitos.
- Pode acrescentar consulta da entidade vinculada ao contexto da conversa.
- Remove ferramentas repetidas e limita o plano a cinco passos.
- Ignora instruções que peçam tenant explícito, pagamento, compra, exclusão, alteração ou outros padrões bloqueados.
- Solicitações de fornecedor sem produto e unidade resolvidos não entram no plano.

## Execução segura

```text
pergunta + contexto autenticado
          |
          v
planificador determinístico -> até 5 passos independentes
          |
          v
validação: somente Read + objeto JSON + sem restaurantId
          |
          v
registry allowlist -> papel/tenant/especialista/prazo -> ferramenta
          |
          v
resumo + evidências + avisos de falha parcial
```

Execução limitada a 90 segundos, com uma ferramenta por vez para preservar a segurança dos serviços scoped e do `DbContext` do EF Core. Dependências entre etapas são rejeitadas. O tenant sempre vem da identidade autenticada. Falhas por ferramenta aparecem como aviso e não apagam resultados válidos das demais etapas.

## Automação agendada

`AutomationRunner` expira políticas e registra execuções simuladas com chave idempotente, evidências e correlation ID. Mesmo políticas ativas não alteram dados nem chamam serviços externos nesta implementação.

## Implementação

`ConstrainedOrchestrationService.cs`, `OrchestrationContracts.cs`, `AgentToolRegistry.cs`, `AutomationService.cs`, `AutomationRunner.cs` e `AgentsBackgroundScheduler.cs`.
