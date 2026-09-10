using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface ITmpAuthorizationRepository
{
    Task<TmpAuthorization?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TmpAuthorization?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TmpAuthorization>> ListByRestaurantAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<TmpAuthorization> CreateAsync(TmpAuthorization entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TmpAuthorization entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
