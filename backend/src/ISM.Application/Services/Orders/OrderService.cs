using ISM.Application.DTOs.Orders;
using ISM.Application.Interfaces.Orders;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Orders;

namespace ISM.Application.Services.Orders;

public sealed class OrderService(IOrderRepository orders, IDishRepository dishes, ICurrentUser currentUser) : IOrderService
{
    private int RestaurantId => currentUser.RestaurantId is > 0 ? currentUser.RestaurantId.Value : throw new UnauthorizedAccessException("É necessário estar vinculado a um restaurante.");

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await orders.GetAllAsync(RestaurantId, cancellationToken)).Select(Map).ToArray();

    public async Task<OrderResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(RestaurantId, id, cancellationToken);
        return order is null ? null : Map(order);
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = RestaurantId;
        if (request.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.TableReference) || request.TableReference.Trim().Length > 100 || request.Items is null || request.Items.Count is < 1 or > 100)
            throw new InvalidOperationException("Informe a comanda, os itens e um identificador de requisição válido.");
        if (currentUser.Role == IsmRoles.Chef) throw new UnauthorizedAccessException("Este perfil não pode criar pedidos.");

        var existing = await orders.GetByRequestIdAsync(restaurantId, request.RequestId, cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, request);
            return Map(existing);
        }
        var order = new Order { RestaurantId = restaurantId, RequestId = request.RequestId, TableReference = request.TableReference.Trim() };
        foreach (var item in request.Items)
        {
            if (item.DishId <= 0 || item.Quantity is < 1 or > 1000 || item.Observation?.Length > 500)
                throw new InvalidOperationException("Os itens do pedido são inválidos.");
            var dish = await dishes.GetDishByIdAsync(item.DishId, cancellationToken);
            if (dish is null || dish.RestaurantId != restaurantId || !dish.IsActive)
                throw new InvalidOperationException("Um dos pratos não está disponível neste restaurante.");
            order.Items.Add(new OrderItem { RestaurantId = restaurantId, DishId = dish.Id, DishName = dish.Name, Quantity = item.Quantity, UnitPrice = dish.Price, UnitCost = dish.Cost, Observation = item.Observation?.Trim() ?? string.Empty });
        }
        var saved = await orders.AddAsync(order, cancellationToken);
        EnsureSameRequest(saved, request);
        return Map(saved);
    }

    public async Task<OrderResponse?> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(RestaurantId, id, cancellationToken);
        if (order is null) return null;
        if (order.Version != request.Version) throw new InvalidOperationException("O pedido foi alterado. Atualize a tela e tente novamente.");
        var allowed = (order.Status, request.Status) switch
        {
            (OrderStatus.Submitted, OrderStatus.Preparing or OrderStatus.Cancelled) => true,
            (OrderStatus.Preparing, OrderStatus.Ready or OrderStatus.Cancelled) => true,
            (OrderStatus.Ready, OrderStatus.Completed or OrderStatus.Cancelled) => true,
            (OrderStatus.Completed, OrderStatus.Paid or OrderStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException("Transição de status não permitida.");
        if (currentUser.Role == IsmRoles.Chef && request.Status is not (OrderStatus.Preparing or OrderStatus.Ready))
            throw new UnauthorizedAccessException("Este perfil só pode atualizar o preparo do pedido.");
        if (currentUser.Role == IsmRoles.Waiter && request.Status is OrderStatus.Preparing or OrderStatus.Ready)
            throw new UnauthorizedAccessException("Este perfil não pode atualizar o preparo do pedido.");
        order.Status = request.Status;
        order.PaidAtUtc = request.Status == OrderStatus.Paid ? DateTime.UtcNow : null;
        order.UpdatedAtUtc = DateTime.UtcNow;
        order.Version++;
        await orders.SaveChangesAsync(cancellationToken);
        return Map(order);
    }

    private static void EnsureSameRequest(Order order, CreateOrderRequest request)
    {
        var stored = order.Items.Select(item => (item.DishId, item.Quantity, item.Observation)).OrderBy(item => item.DishId).ThenBy(item => item.Quantity).ThenBy(item => item.Observation).ToArray();
        var requested = request.Items.Select(item => (item.DishId, item.Quantity, Observation: item.Observation?.Trim() ?? string.Empty)).OrderBy(item => item.DishId).ThenBy(item => item.Quantity).ThenBy(item => item.Observation).ToArray();
        if (order.TableReference != request.TableReference.Trim() || !stored.SequenceEqual(requested))
            throw new InvalidOperationException("O identificador da requisição já foi usado para outro pedido.");
    }

    private static OrderResponse Map(Order order) => new(order.Id, order.RequestId, order.TableReference, order.Status, order.CreatedAtUtc, order.PaidAtUtc, order.Version,
        order.Items.Select(item => new OrderItemResponse(item.DishId, item.DishName, item.Quantity, item.UnitPrice, item.UnitCost, item.Observation)).ToArray());
}
