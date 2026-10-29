using System.ComponentModel.DataAnnotations;
using ISM.Domain.Modules.Orders;

namespace ISM.Application.DTOs.Orders;

public sealed class CreateOrderRequest
{
    [Required] public Guid RequestId { get; set; }
    [Required, StringLength(100, MinimumLength = 1)] public string TableReference { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(100)] public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public sealed class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)] public int DishId { get; set; }
    [Range(1, 1000)] public int Quantity { get; set; }
    [StringLength(500)] public string? Observation { get; set; }
}

public sealed record UpdateOrderStatusRequest(OrderStatus Status, int Version);
public sealed record OrderItemResponse(int DishId, string DishName, int Quantity, decimal UnitPrice, decimal UnitCost, string Observation);
public sealed record OrderResponse(int Id, Guid RequestId, string TableReference, OrderStatus Status, DateTime CreatedAtUtc, DateTime? PaidAtUtc, int Version, IReadOnlyList<OrderItemResponse> Items);
