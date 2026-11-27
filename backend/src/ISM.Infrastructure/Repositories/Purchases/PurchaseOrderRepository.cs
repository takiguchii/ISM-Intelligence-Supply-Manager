using ISM.Domain.Interfaces.Purchases;
using ISM.Domain.Modules.Purchases.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Purchases;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly IsmDbContext _context;

    public PurchaseOrderRepository(IsmDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(po => po.RestaurantId == restaurantId && po.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .ThenInclude(i => i.Product)
            .Where(po => po.RestaurantId == restaurantId)
            .OrderByDescending(po => po.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenOrdersForProductAsync(int restaurantId, int productId, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Where(po => po.RestaurantId == restaurantId
                && (po.Status == PurchaseOrderStatus.Draft || po.Status == PurchaseOrderStatus.Sent || po.Status == PurchaseOrderStatus.PartiallyDelivered))
            .AnyAsync(po => po.Items.Any(i => i.ProductId == productId), cancellationToken);
    }

    public async Task<PurchaseOrder> AddAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        await _context.PurchaseOrders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        _context.PurchaseOrders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
