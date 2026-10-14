using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Enums;

namespace ISM.Domain.Modules.Stock.Entities;

public sealed class StockMovement : BaseEntity
{
    [Key]
    public new int Id { get; set; }

    public int RestaurantId { get; set; }
    [ForeignKey(nameof(RestaurantId))]
    public Restaurant? Restaurant { get; set; }

    public int ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    public StockMovementType MovementType { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal QuantityDelta { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? UnitCostSnapshot { get; set; }

    public Guid? TriggeredByImportId { get; set; }
    public int? TriggeredByUserId { get; set; }
}
