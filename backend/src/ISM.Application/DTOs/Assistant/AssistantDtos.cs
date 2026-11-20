using System.ComponentModel.DataAnnotations;
using ISM.Application.Agents;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Application.DTOs.Assistant;

public sealed record AssistantActionResponse(
    int Id, int AlertId, string Specialist, string ActionType, string EntityType, int EntityId,
    string EntityName, AssistantProposalStatus Status, decimal OriginalValue, decimal ProposedValue,
    decimal SnapshotCost, decimal? SnapshotTargetMarginPercent, string EvidenceJson,
    DateTime GeneratedAtUtc, int? DecidedByUserId, DateTime? DecidedAtUtc,
    DateTime ExpiresAtUtc, int Version);

public sealed record AssistantStatusResponse(bool Enabled, bool ProviderReachable, string Model, string? Message);
public sealed record AssistantOverviewResponse(AssistantStatusResponse Status, int PendingActions, int UnreadAlerts, IReadOnlyList<AssistantActionResponse> Actions);
public sealed record AssistantChatRequest([Required, StringLength(1000)] string Message);
public sealed record AssistantChatResponse(string Mode, string Message, string Source, string? Warning)
{
    public string? Specialist { get; init; }
    public string? ToolName { get; init; }
    public AgentDecisionCode? DecisionCode { get; init; }
    public IReadOnlyList<EvidenceRef> Evidence { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed record AssistantContextResponse(
    int AlertId,
    string AlertType,
    string Severity,
    string Title,
    string Message,
    string? ReferenceEntityType,
    int? ReferenceEntityId,
    string EntityName,
    IReadOnlyDictionary<string, string> Facts,
    DateTime GeneratedAtUtc);

public sealed record AssistantThreadResponse(
    int Id,
    int? ParentThreadId,
    int? OriginAlertId,
    string OriginType,
    string Subject,
    AssistantContextResponse? Context,
    DateTime CreatedAtUtc,
    DateTime LastMessageAtUtc);

public sealed record AssistantMessageResponse(
    int Id,
    string Role,
    string Content,
    string? Mode,
    string? Source,
    DateTime CreatedAtUtc);

public sealed record AssistantCreateThreadRequest(
    int? OriginAlertId = null,
    int? ParentThreadId = null,
    string? Subject = null);

public sealed record AssistantSendMessageRequest(
    [Required, StringLength(1000, MinimumLength = 1)] string Message);

public sealed record AssistantConversationResponse(
    AssistantThreadResponse Thread,
    IReadOnlyList<AssistantMessageResponse> Messages);
