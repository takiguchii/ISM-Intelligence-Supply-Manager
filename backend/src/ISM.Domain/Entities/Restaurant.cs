using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Stock.Entities;

namespace ISM.Domain.Entities;

public sealed class Restaurant : BaseEntity
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string CNPJ { get; set; } = string.Empty;
    public DateTime Created { get; set; }

    public int? PlanId { get; set; }
    [ForeignKey(nameof(PlanId))]
    public Plan? Plan { get; set; }

    public DateTime? TrialEndAtUtc { get; set; }
    public bool IsPlanActive { get; set; } = true;

    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Dish> Dishes { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];

    [StringLength(80)]
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    [RegularExpression("^(BRL|USD|EUR)$")]
    public string Currency { get; set; } = "BRL";
    [RegularExpression("^(pt-BR|en-US|es-ES)$")]
    public string Language { get; set; } = "pt-BR";
    [StringLength(300)]
    public string? Address { get; set; }
    [StringLength(30)]
    public string? Phone { get; set; }
    [EmailAddress, StringLength(150)]
    public string? ContactEmail { get; set; }
    [StringLength(1000)]
    public string? OpeningHours { get; set; }
    [StringLength(750000)]
    [RegularExpression(@"^data:image/(png|jpeg|webp);base64,[A-Za-z0-9+/=]+$", ErrorMessage = "Use uma imagem PNG, JPEG ou WebP.")]
    public string? LogoDataUrl { get; set; }
    [Range(0, 100)]
    public decimal ServiceFeePercent { get; set; }
    [Range(0, 10000)]
    public decimal DeliveryFee { get; set; }
    [Range(0, 10000)]
    public decimal PackagingFee { get; set; }

    public string? PhotoImportApiKey { get; set; }
    public string? PhotoImportModel { get; set; }
    public string? PhotoImportApiEndpoint { get; set; }
}