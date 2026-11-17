using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Domain.Modules.Assistant.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Assistant;

public sealed class AutomationRepository(IsmDbContext db) : IAutomationRepository
{
    public async Task<IReadOnlyList<AutomationPolicy>> ListAsync(int restaurantId, CancellationToken cancellationToken = default)
        => await db.AutomationPolicies.AsNoTracking().Where(p => p.RestaurantId == restaurantId)
            .OrderBy(p => p.Status).ThenBy(p => p.Name).ToListAsync(cancellationToken);

    public Task<AutomationPolicy?> GetAsync(int id, int restaurantId, CancellationToken cancellationToken = default)
        => db.AutomationPolicies.FirstOrDefaultAsync(p => p.Id == id && p.RestaurantId == restaurantId, cancellationToken);

    public async Task<AutomationPolicy> AddAsync(AutomationPolicy policy, CancellationToken cancellationToken = default)
    {
        db.AutomationPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<AutomationRun>> ListRunsAsync(int policyId, int restaurantId, CancellationToken cancellationToken = default)
        => await db.AutomationRuns.AsNoTracking().Where(r => r.PolicyId == policyId && r.RestaurantId == restaurantId)
            .OrderByDescending(r => r.StartedAtUtc).Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AutomationPolicy>> GetDueAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default)
        => await db.AutomationPolicies.Where(p => p.RestaurantId == restaurantId && p.Status == AutomationPolicyStatus.Active
                && p.NextRunAtUtc <= nowUtc && (p.ExpiresAtUtc == null || p.ExpiresAtUtc > nowUtc)
                && p.RunsThisPeriod < p.MaxRunsPerPeriod)
            .OrderBy(p => p.NextRunAtUtc).Take(50).ToListAsync(cancellationToken);

    public async Task<int> ExpireAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var policies = await db.AutomationPolicies.Where(p => p.RestaurantId == restaurantId
                && p.Status == AutomationPolicyStatus.Active && p.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
        foreach (var policy in policies)
        {
            policy.Status = AutomationPolicyStatus.Expired;
            policy.UpdatedAtUtc = nowUtc;
            policy.Version++;
        }
        if (policies.Count > 0) await db.SaveChangesAsync(cancellationToken);
        return policies.Count;
    }

    public async Task<bool> TryClaimAsync(AutomationPolicy policy, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        policy.LastRunAtUtc = nowUtc;
        policy.NextRunAtUtc = nowUtc.AddMinutes(policy.IntervalMinutes);
        policy.RunsThisPeriod++;
        policy.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(policy).State = EntityState.Detached;
            return false;
        }
    }

    public async Task AddRunAsync(AutomationRun run, CancellationToken cancellationToken = default)
    {
        db.AutomationRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
    }
}
