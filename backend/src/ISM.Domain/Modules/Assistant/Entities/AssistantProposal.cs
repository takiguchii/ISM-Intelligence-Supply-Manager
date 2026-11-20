using ISM.Domain.Entities;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AssistantProposal : BaseEntity
{
    public int RestaurantId { get; set; }
    public int AlertId { get; set; }
    public string Specialist { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public AssistantProposalStatus Status { get; set; }
    public decimal OriginalValue { get; set; }
    public decimal ProposedValue { get; set; }
    public decimal SnapshotCost { get; set; }
    public decimal? SnapshotTargetMarginPercent { get; set; }
    public string EvidenceJson { get; set; } = "{}";
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public int Version { get; set; }
}
