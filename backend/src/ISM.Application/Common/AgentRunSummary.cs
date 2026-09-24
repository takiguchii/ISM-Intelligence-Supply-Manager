namespace ISM.Application.Common;

public sealed record AgentRunSummary(
    int RestaurantsProcessed,
    int EntitiesEvaluated,
    int AlertsCreated,
    int AlertsSkippedByDedup,
    TimeSpan Duration,
    string AgentName);
