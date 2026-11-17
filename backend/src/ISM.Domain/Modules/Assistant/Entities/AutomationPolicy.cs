using ISM.Domain.Entities;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AutomationPolicy : BaseEntity
{
    public int RestaurantId { get; set; }
    public int OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialist { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    public int IntervalMinutes { get; set; }
    public AutomationPolicyStatus Status { get; set; } = AutomationPolicyStatus.Draft;
    public AutomationMode Mode { get; set; } = AutomationMode.DryRun;
    public DateTime StartsAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastRunAtUtc { get; set; }
    public DateTime NextRunAtUtc { get; set; }
    public int RunsThisPeriod { get; set; }
    public int MaxRunsPerPeriod { get; set; } = 31;
    public int Version { get; set; }
}
