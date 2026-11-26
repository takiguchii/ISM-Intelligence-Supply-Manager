using System.Text.Json;

namespace ISM.Application.Agents;

public sealed record AssistantQueryContext(
    string Question,
    int UserId,
    int RestaurantId,
    string Role,
    string? SourceThreadId = null,
    string? SubjectEntityType = null,
    int? SubjectEntityId = null);

public sealed record OrchestrationPlanStep(
    string StepId,
    string Specialist,
    string ToolName,
    JsonElement Input,
    IReadOnlyList<string> DependsOn);

public sealed record ValidatedOrchestrationPlan(
    string CorrelationId,
    AssistantQueryContext Query,
    IReadOnlyList<OrchestrationPlanStep> Steps,
    int MaxSteps = 5,
    int MaxDepth = 2,
    int BudgetMilliseconds = 90000);

public sealed record OrchestrationStepResult(
    OrchestrationPlanStep Step,
    AgentToolResult Result);

public sealed record OrchestrationResult(
    string CorrelationId,
    IReadOnlyList<OrchestrationStepResult> Steps,
    string Answer,
    bool Partial,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EvidenceRef> Evidence);

public interface IOrchestrationPlanner
{
    Task<ValidatedOrchestrationPlan> PlanAsync(AssistantQueryContext query, CancellationToken cancellationToken = default);
}

public interface IOrchestrationExecutor
{
    Task<OrchestrationResult> ExecuteAsync(ValidatedOrchestrationPlan plan, CancellationToken cancellationToken = default);
}

public interface IOrchestrationSynthesizer
{
    OrchestrationResult Summarize(ValidatedOrchestrationPlan plan, IReadOnlyList<OrchestrationStepResult> steps);
}
