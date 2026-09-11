using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Stock.Entities;

public sealed class Product : BaseEntity
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    [ForeignKey(nameof(RestaurantId))]
    public Restaurant? Restaurant { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal MaximumQuantity { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal AverageCost { get; set; }
    public decimal MovingAverageConsumption { get; set; }
    public DateTime? LastConsumptionRecalculatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
