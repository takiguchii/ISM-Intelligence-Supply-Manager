using System.ComponentModel.DataAnnotations;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Application.DTOs.Assistant;

public sealed record AutomationPolicyResponse(
    int Id, string Name, string Specialist, string ActionType, string Target, string TimeZone,
    int IntervalMinutes, AutomationPolicyStatus Status, AutomationMode Mode, DateTime StartsAtUtc,
    DateTime? ExpiresAtUtc, DateTime? LastRunAtUtc, DateTime NextRunAtUtc, int RunsThisPeriod,
    int MaxRunsPerPeriod, int Version);

public sealed record CreateAutomationPolicyRequest(
    [Required, StringLength(120)] string Name,
    [Required, StringLength(40)] string Specialist,
    [Required, StringLength(80)] string ActionType,
    [Required, StringLength(120)] string Target,
    [StringLength(80)] string? TimeZone,
    [Range(5, 10080)] int IntervalMinutes,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    [Range(1, 31)] int MaxRunsPerPeriod = 31);

public sealed record ConfirmAutomationRequest(bool Confirm, bool? ActiveMode = null);

public sealed record AutomationRunResponse(
    int Id, int PolicyId, AutomationRunStatus Status, string IdempotencyKey,
    DateTime ScheduledAtUtc, DateTime StartedAtUtc, DateTime? FinishedAtUtc,
    string Reason, string EvidenceJson, string? CorrelationId);
