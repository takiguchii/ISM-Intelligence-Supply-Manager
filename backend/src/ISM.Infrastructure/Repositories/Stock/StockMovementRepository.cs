using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Stock;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly IsmDbContext _dbContext;

    public StockMovementRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockMovement?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(
        int restaurantId,
        int productId,
        DateTime? sinceUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId && m.ProductId == productId);

        if (sinceUtc.HasValue)
            query = query.Where(m => m.CreatedAtUtc >= sinceUtc.Value);

        return await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetPagedByProductAsync(
        int restaurantId,
        int productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId && m.ProductId == productId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> SumNegativeDeltasLast30DaysAsync(
        int restaurantId,
        IEnumerable<int> productIds,
        DateTime sinceUtc,
        StockMovementType[] allowedTypes,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<int, decimal>();

        var allowed = allowedTypes.Cast<int>().ToArray();

        var rows = await _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m =>
                m.RestaurantId == restaurantId &&
                ids.Contains(m.ProductId) &&
                m.CreatedAtUtc >= sinceUtc &&
                allowed.Contains((int)m.MovementType))
            .GroupBy(m => m.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Sum = g.Sum(m => m.QuantityDelta < 0 ? -m.QuantityDelta : 0m)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProductId, r => r.Sum);
    }

    public async Task<StockMovement> AddAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<StockMovement>().Add(movement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return movement;
    }

    public async Task BulkAddAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken = default)
    {
        var list = movements.ToList();
        if (list.Count == 0) return;

        _dbContext.Set<StockMovement>().AddRange(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
