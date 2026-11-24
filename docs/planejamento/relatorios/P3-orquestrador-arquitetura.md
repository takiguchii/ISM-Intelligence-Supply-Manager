# P3 — Arquitetura do orquestrador geral

## Gate

**PASS**. O contrato foi separado em planejador, executor e sintetizador dentro da camada Application:

- `AssistantQueryContext` recebe pergunta e identidade autenticada do servidor.
- `ValidatedOrchestrationPlan` limita a cinco etapas, profundidade dois e orçamento global de 90 segundos.
- `OrchestrationStepResult` preserva código de decisão, dados e evidências da ferramenta.
- `OrchestrationResult` marca análise parcial e avisos sem transformar falhas em fatos.

O executor só aceita ferramentas de leitura presentes no registry, rejeita `restaurantId` vindo do payload, mantém o correlation ID e limita concorrência a duas etapas. Operações de escrita, aprovação e autoação não entram no plano.

## Sequência implementada

```text
thread general → planner determinístico → validação allowlist/RBAC
    → até 2 ferramentas independentes em paralelo → evidências tipadas
    → síntese determinística parcial ou completa → resposta do chat
```

O LLM permanece opcional para conversação livre; fatos e números da orquestração vêm exclusivamente das ferramentas autorizadas.

## Validação

`dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore` — PASS, 0 warnings e 0 erros.

Limitações assumidas: a aplicação da migration requer banco de teste isolado; o provider Ollama e as notificações Telegram dependem de configuração externa segura.
