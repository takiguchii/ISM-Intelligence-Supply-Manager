using ISM.Application.DTOs.Assistant;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Domain.Modules.Assistant.Enums;

namespace ISM.Application.Services.Assistant;

public sealed class AutomationService(
    IAutomationRepository repository,
    ICurrentUser currentUser,
    TimeProvider clock) : IAutomationService
{
    private static readonly IReadOnlyDictionary<string, string> AllowedActions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["stock.scanalerts"] = "stock",
        ["pricing.scancmv"] = "pricing",
        ["suppliers.scanoffers"] = "suppliers",
        ["finance.summary"] = "finance"
    };

    public async Task<IReadOnlyList<AutomationPolicyResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var policies = await repository.ListAsync(RequireRestaurant(), cancellationToken);
        var now = Now();
        var expired = false;
        foreach (var policy in policies.Where(p => p.Status == AutomationPolicyStatus.Active && p.ExpiresAtUtc <= now))
        {
            policy.Status = AutomationPolicyStatus.Expired;
            policy.UpdatedAtUtc = now;
            expired = true;
        }
        if (expired) await repository.SaveAsync(cancellationToken);
        return policies.Select(Map).ToList();
    }

    public async Task<AutomationPolicyResponse?> CreateAsync(CreateAutomationPolicyRequest request, CancellationToken cancellationToken = default)
    {
        var specialist = request.Specialist.Trim().ToLowerInvariant();
        var action = request.ActionType.Trim().ToLowerInvariant();
        if (!AllowedActions.TryGetValue($"{specialist}.{action}", out var owner) || owner != specialist) return null;

        var now = Now();
        var starts = request.StartsAtUtc?.ToUniversalTime() ?? now;
        var expires = request.ExpiresAtUtc?.ToUniversalTime();
        if (expires.HasValue && expires <= starts) return null;
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(request.TimeZone) ? "America/Sao_Paulo" : request.TimeZone.Trim()); }
        catch (TimeZoneNotFoundException) { return null; }
        catch (InvalidTimeZoneException) { return null; }

        var policy = await repository.AddAsync(new AutomationPolicy
        {
            RestaurantId = RequireRestaurant(), OwnerUserId = currentUser.UserId,
            Name = request.Name.Trim(), Specialist = specialist, ActionType = action,
            Target = request.Target.Trim(), TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "America/Sao_Paulo" : request.TimeZone.Trim(),
            IntervalMinutes = request.IntervalMinutes, Status = AutomationPolicyStatus.Draft,
            Mode = AutomationMode.DryRun, StartsAtUtc = starts, ExpiresAtUtc = expires,
            NextRunAtUtc = starts, MaxRunsPerPeriod = request.MaxRunsPerPeriod, Version = 0
        }, cancellationToken);
        return Map(policy);
    }

    public async Task<(AutomationPolicyResponse? Policy, int StatusCode)> ConfirmAsync(int id, ConfirmAutomationRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Confirm) return (null, 400);
        var policy = await repository.GetAsync(id, RequireRestaurant(), cancellationToken);
        if (policy is null) return (null, 404);
        if (policy.Status != AutomationPolicyStatus.Draft) return (Map(policy), 409);
        var now = Now();
        if (policy.ExpiresAtUtc <= now)
        {
            policy.Status = AutomationPolicyStatus.Expired;
            policy.UpdatedAtUtc = now;
            await repository.SaveAsync(cancellationToken);
            return (Map(policy), 409);
        }
        policy.Status = AutomationPolicyStatus.Active;
        policy.Mode = request.ActiveMode == true ? AutomationMode.Active : AutomationMode.DryRun;
        policy.UpdatedAtUtc = now;
        policy.Version++;
        await repository.SaveAsync(cancellationToken);
        return (Map(policy), 200);
    }

    public Task<AutomationPolicyResponse?> PauseAsync(int id, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, AutomationPolicyStatus.Paused, cancellationToken);

    public Task<AutomationPolicyResponse?> RevokeAsync(int id, CancellationToken cancellationToken = default)
        => ChangeStatusAsync(id, AutomationPolicyStatus.Revoked, cancellationToken);

    public async Task<IReadOnlyList<AutomationRunResponse>> RunsAsync(int id, CancellationToken cancellationToken = default)
        => (await repository.ListRunsAsync(id, RequireRestaurant(), cancellationToken)).Select(Map).ToList();

    private async Task<AutomationPolicyResponse?> ChangeStatusAsync(int id, AutomationPolicyStatus status, CancellationToken cancellationToken)
    {
        var policy = await repository.GetAsync(id, RequireRestaurant(), cancellationToken);
        if (policy is null) return null;
        if (policy.Status is AutomationPolicyStatus.Revoked or AutomationPolicyStatus.Expired) return Map(policy);
        policy.Status = status;
        policy.UpdatedAtUtc = Now();
        policy.Version++;
        await repository.SaveAsync(cancellationToken);
        return Map(policy);
    }

    private int RequireRestaurant() => currentUser is { IsManagerOrAbove: true, RestaurantId: > 0 } ? currentUser.RestaurantId.Value : throw new UnauthorizedAccessException("Restaurante gerencial não identificado.");
    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
    private static AutomationPolicyResponse Map(AutomationPolicy p) => new(p.Id, p.Name, p.Specialist, p.ActionType, p.Target, p.TimeZone, p.IntervalMinutes, p.Status, p.Mode, p.StartsAtUtc, p.ExpiresAtUtc, p.LastRunAtUtc, p.NextRunAtUtc, p.RunsThisPeriod, p.MaxRunsPerPeriod, p.Version);
    private static AutomationRunResponse Map(AutomationRun r) => new(r.Id, r.PolicyId, r.Status, r.IdempotencyKey, r.ScheduledAtUtc, r.StartedAtUtc, r.FinishedAtUtc, r.Reason, r.EvidenceJson, r.CorrelationId);
}
