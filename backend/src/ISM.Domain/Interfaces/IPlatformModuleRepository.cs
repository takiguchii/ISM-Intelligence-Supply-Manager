using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface IPlatformModuleRepository
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    Task<int> CountEnabledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformModule>> GetEnabledOrderedAsync(CancellationToken cancellationToken = default);
    Task<PlatformModule?> GetFirstEnabledAsync(CancellationToken cancellationToken = default);
}
