# Evidência P3

Os contratos `IOrchestrationPlanner`, `IOrchestrationExecutor` e `IOrchestrationSynthesizer` estão em `backend/src/ISM.Application/Agents/OrchestrationContracts.cs`. A implementação `ConstrainedOrchestrationService` fica em Application e é registrada no DI da API, sem dependência de Infrastructure no Domain.

Critérios verificados: limite de passos, allowlist de leitura, rejeição de tenant no input, correlation ID, cancelamento/orçamento e preservação de falha parcial.
