using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class RestaurantDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome não pode exceder 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "O CNPJ deve conter 14 caracteres.")]
    [RegularExpression(@"^\d{14}$", ErrorMessage = "O CNPJ deve conter apenas 14 dígitos.")]
    public string CNPJ { get; set; } = string.Empty;

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

    public string? PlanName { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsPlanActive { get; set; }
    public DateTime? TrialEndAtUtc { get; set; }
}