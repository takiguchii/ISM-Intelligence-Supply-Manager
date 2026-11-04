using ISM.Application.DTOs.Orders;

namespace ISM.Application.Interfaces.Orders;

public interface IOrderService
{
    Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrderPage> GetAdminPageAsync(int pageNumber = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse?> UpdateAsync(int id, UpdateOrderRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
