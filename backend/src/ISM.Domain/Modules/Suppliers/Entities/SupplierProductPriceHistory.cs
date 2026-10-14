using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Suppliers.Entities;

public sealed class SupplierProductPriceHistory : BaseEntity
{
    [Key]
    public new int Id { get; set; }

    public int RestaurantId { get; set; }
    [ForeignKey(nameof(RestaurantId))]
    public Restaurant? Restaurant { get; set; }

    public int? SupplierId { get; set; }
    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required]
    [MaxLength(200)]
    public string SupplierRawName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductRawName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal QuantityPurchased { get; set; }

    [MaxLength(200)]
    public string? NFeAccessKeyOrImportId { get; set; }

    [Column(TypeName = "datetime(6)")]
    public DateTime PurchasedAtUtc { get; set; }
}
