using ISM.Domain.Modules.Purchases.Entities;

namespace ISM.Domain.Interfaces.Purchases;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenOrdersForProductAsync(int restaurantId, int productId, CancellationToken cancellationToken = default);
    Task<PurchaseOrder> AddAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
}
