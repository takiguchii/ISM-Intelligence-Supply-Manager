using System.ComponentModel.DataAnnotations;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Entities;

namespace ISM.Domain.Modules.Purchases.Entities;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Sent = 1,
    PartiallyDelivered = 2,
    Completed = 3,
    Cancelled = 4
}

public sealed class PurchaseOrder : BaseEntity
{
    public int RestaurantId { get; set; }
    public int SupplierId { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal TotalAmount { get; set; }
    public DateTime? ExpectedDeliveryAtUtc { get; set; }
    [MaxLength(500)]
    public string Observation { get; set; } = string.Empty;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public sealed class PurchaseOrderItem : BaseEntity
{
    public int RestaurantId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitPrice { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Product? Product { get; set; }
}
