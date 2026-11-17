using ISM.Domain.Entities;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AutomationRun : BaseEntity
{
    public int RestaurantId { get; set; }
    public int PolicyId { get; set; }
    public AutomationRunStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "{}";
    public string? CorrelationId { get; set; }
}
