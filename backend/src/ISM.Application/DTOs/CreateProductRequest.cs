using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record CreateProductRequest(
    [Required]
    int RestaurantId,
    [Required]
    [MaxLength(120)]
    string Name,
    [Required]
    [MaxLength(10)]
    string Unit,
    [Range(0, double.MaxValue)]
    decimal CurrentQuantity,
    [Range(0, double.MaxValue)]
    decimal MinimumQuantity,
    [Range(0, double.MaxValue)]
    decimal AverageCost);
