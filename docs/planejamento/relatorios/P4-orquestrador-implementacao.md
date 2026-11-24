# P4 — Implementação do orquestrador geral

## Resultado

**PASS** para o caminho determinístico de threads `general` e consultas diretas sem mutação.

Foi implementado `ConstrainedOrchestrationService` na camada Application. Ele:

- cria planos tipados a partir de domínios reconhecidos;
- aceita somente ferramentas de leitura registradas e autorizadas;
- rejeita tenant no payload, solicitações de escrita e padrões de prompt injection;
- executa no máximo cinco etapas, com concorrência máxima de duas e orçamento global de 90 segundos;
- mantém correlation ID, cancelamento, evidências e falha parcial;
- sintetiza apenas resultados retornados pelas ferramentas, sem inventar números;
- é usado pelo `AssistantService` para o Assistente geral e preserva o fluxo especializado.

## Validação

| Comando | Resultado |
|---|---|
| `dotnet build backend/src/ISM.API/ISM.API.csproj --no-restore` | PASS — 0 warnings, 0 erros |
| `dotnet test ... --filter FullyQualifiedName~OrchestrationContractTests` | PASS — 43 casos |
| `git diff --check` | PASS |

As ferramentas continuam read-only; aprovações e automações existentes permanecem fora do plano do orquestrador.

## Limitações

A síntese por Ollama continua opcional e não é necessária para a correção factual das consultas determinísticas. A integração HTTP/DB com migration nova depende de ambiente de teste isolado, não disponível sem substituir o banco compartilhado atual.
