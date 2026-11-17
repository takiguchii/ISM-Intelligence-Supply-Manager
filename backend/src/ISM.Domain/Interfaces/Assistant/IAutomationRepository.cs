using ISM.Domain.Modules.Assistant.Entities;

namespace ISM.Domain.Interfaces;

public interface IAutomationRepository
{
    Task<IReadOnlyList<AutomationPolicy>> ListAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<AutomationPolicy?> GetAsync(int id, int restaurantId, CancellationToken cancellationToken = default);
    Task<AutomationPolicy> AddAsync(AutomationPolicy policy, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutomationRun>> ListRunsAsync(int policyId, int restaurantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutomationPolicy>> GetDueAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<int> ExpireAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> TryClaimAsync(AutomationPolicy policy, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task AddRunAsync(AutomationRun run, CancellationToken cancellationToken = default);
}
