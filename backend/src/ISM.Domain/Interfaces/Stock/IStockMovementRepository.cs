using ISM.Domain.Modules.Stock.Enums;
using ISM.Domain.Modules.Stock.Entities;

namespace ISM.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<StockMovement?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(
        int restaurantId,
        int productId,
        DateTime? sinceUtc = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetPagedByProductAsync(
        int restaurantId,
        int productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, decimal>> SumNegativeDeltasLast30DaysAsync(
        int restaurantId,
        IEnumerable<int> productIds,
        DateTime sinceUtc,
        StockMovementType[] allowedTypes,
        CancellationToken cancellationToken = default);
    Task<StockMovement> AddAsync(StockMovement movement, CancellationToken cancellationToken = default);
    Task BulkAddAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken = default);
}
