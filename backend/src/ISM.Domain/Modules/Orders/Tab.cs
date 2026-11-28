using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Orders;

public sealed class Tab : BaseEntity
{
    public int RestaurantId { get; set; }
    public string TableReference { get; set; } = string.Empty;
    public string? OpenTableReference { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Total { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
