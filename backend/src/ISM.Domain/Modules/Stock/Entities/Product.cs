using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Stock.Entities;

public sealed class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal AverageCost { get; set; }
}
