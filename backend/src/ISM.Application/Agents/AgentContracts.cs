using System.Text.Json;

namespace ISM.Application.Agents;

public enum AgentDecisionCode
{
    CanAct,
    NeedsApproval,
    InsufficientData,
    Denied,
    Unreachable,
    Stale
}

public enum AgentToolOperation
{
    Read,
    Propose,
    Execute
}

public sealed record EvidenceRef(
    string SourceType,
    string SourceId,
    int RestaurantId,
    DateTime ObservedAtUtc,
    DateTime? AsOfUtc,
    string Summary);

public sealed record AgentToolDescriptor(
    string ToolName,
    string Specialist,
    string Version,
    string Purpose,
    AgentToolOperation Operation,
    IReadOnlyCollection<string> RequiredRoles,
    int MaxResults,
    int DeadlineMilliseconds,
    bool RequiresApproval,
    string Risk)
{
    public IReadOnlyDictionary<string, string> InputSchema { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed record AgentToolContext(int UserId, int RestaurantId, string Role, string CorrelationId);

public sealed record AgentToolResult(
    string ToolName,
    AgentDecisionCode Code,
    object? Data,
    IReadOnlyList<EvidenceRef> Evidence,
    string Message,
    string CorrelationId,
    long DurationMilliseconds);

public sealed record AgentToolExecuteRequest(string ToolName, JsonElement Input, string? CorrelationId = null)
{
    public string? Specialist { get; init; }
}

public interface IAgentTool
{
    AgentToolDescriptor Descriptor { get; }
    Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default);
}

public interface IAgentToolRegistry
{
    IReadOnlyCollection<AgentToolDescriptor> List();
    Task<AgentToolResult> ExecuteAsync(AgentToolExecuteRequest request, CancellationToken cancellationToken = default);
}

public sealed record AgentIntent(string Specialist, string ToolName, JsonElement Input, double Confidence);

public interface IAgentIntentRouter
{
    AgentIntent? Resolve(string message);
    AgentIntent? ResolveStructured(JsonElement modelOutput);
}
