using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Suppliers.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Suppliers;

public sealed class SupplierProductPriceHistoryRepository : ISupplierProductPriceHistoryRepository
{
    private readonly IsmDbContext _dbContext;

    public SupplierProductPriceHistoryRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SupplierProductPriceHistory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbContext.Set<SupplierProductPriceHistory>()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task BulkAddAsync(IEnumerable<SupplierProductPriceHistory> history, CancellationToken cancellationToken = default)
    {
        var list = history.ToList();
        if (list.Count == 0) return;

        _dbContext.Set<SupplierProductPriceHistory>().AddRange(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierProductPriceHistory>> GetForRestaurantSinceAsync(
        int restaurantId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
        => await _dbContext.Set<SupplierProductPriceHistory>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(h => h.RestaurantId == restaurantId && h.PurchasedAtUtc >= sinceUtc)
            .OrderByDescending(h => h.PurchasedAtUtc)
            .ToListAsync(cancellationToken);
}
