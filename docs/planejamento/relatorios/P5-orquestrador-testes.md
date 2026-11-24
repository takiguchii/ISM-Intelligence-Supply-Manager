# P5 — Testes do orquestrador e autocorreção

## Resultado

**PASS**. A matriz automatizada contém 40 cenários distintos: 15 positivos, 15 negativos e 10 adversos, além de testes de execução composta, falha parcial e rejeição de payload de tenant/escrita.

As asserções verificam: limite de cinco passos, ausência de `restaurantId` no input, allowlist de leitura, preservação de evidências, resposta parcial quando uma fonte falha e zero chamada ao registry para plano proibido.

Durante a regressão, uma expectativa antiga de origem da resposta financeira detectou que o novo caminho estava interceptando consulta de domínio único. A causa foi corrigida preservando o fluxo especializado para planos com uma etapa; o orquestrador é usado quando existe coordenação entre especialistas.

## Evidências

| Comando | Resultado |
|---|---|
| `dotnet test ... --filter FullyQualifiedName~OrchestrationContractTests` | PASS — 43 casos |
| `dotnet test ... --filter FullyQualifiedName~AssistantApprovalTests` | PASS — 22 casos |
| `dotnet test backend/ISM.sln --no-restore` | PASS após correção — 1 arquitetura, 256 unitários, 51 integração |

Nenhum cenário de segurança ou mutação não autorizada ficou aberto.
