using ISM.Domain.Modules.Orders;

namespace ISM.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default);
    Task<Order?> GetByRequestIdAsync(int restaurantId, Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
