using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Orders;

public enum OrderStatus { Submitted, Preparing, Ready, Completed, Paid, Cancelled }

public sealed class Order : BaseEntity
{
    public int RestaurantId { get; set; }
    public Guid RequestId { get; set; }
    public string TableReference { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    public DateTime? PaidAtUtc { get; set; }
    public int Version { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public sealed class OrderItem : BaseEntity
{
    public int RestaurantId { get; set; }
    public int OrderId { get; set; }
    public int DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public string Observation { get; set; } = string.Empty;
    public Order? Order { get; set; }
}
