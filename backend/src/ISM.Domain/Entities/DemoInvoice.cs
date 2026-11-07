namespace ISM.Domain.Entities;

public sealed class DemoInvoice
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
