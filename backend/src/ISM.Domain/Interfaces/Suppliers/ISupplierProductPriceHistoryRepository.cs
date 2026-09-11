using ISM.Domain.Modules.Suppliers.Entities;

namespace ISM.Domain.Interfaces;

public interface ISupplierProductPriceHistoryRepository
{
    Task<SupplierProductPriceHistory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task BulkAddAsync(IEnumerable<SupplierProductPriceHistory> history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierProductPriceHistory>> GetForRestaurantSinceAsync(
        int restaurantId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
}
