using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AssistantActionAudit : BaseEntity
{
    public int RestaurantId { get; set; }
    public int ProposalId { get; set; }
    public int ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public decimal? ValueBefore { get; set; }
    public decimal? ValueAfter { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
