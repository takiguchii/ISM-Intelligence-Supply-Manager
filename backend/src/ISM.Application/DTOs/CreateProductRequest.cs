using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record CreateProductRequest(
    [property: Required]
    [property: MaxLength(120)]
    string Name,
    [property: Required]
    [property: MaxLength(10)]
    string Unit,
    [property: Range(0, double.MaxValue)]
    decimal CurrentQuantity,
    [property: Range(0, double.MaxValue)]
    decimal MinimumQuantity,
    [property: Range(0, double.MaxValue)]
    decimal AverageCost);
