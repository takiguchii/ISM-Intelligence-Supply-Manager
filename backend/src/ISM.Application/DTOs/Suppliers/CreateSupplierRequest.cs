using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record CreateSupplierRequest(
    int RestaurantId,
    [property: Required]
    [property: MaxLength(150)]
    string Name,
    [property: Required]
    [property: MaxLength(100)]
    string Category,
    [property: MaxLength(500)]
    string? Description,
    [property: Required]
    [property: EmailAddress]
    [property: MaxLength(150)]
    string Email,
    [property: Required]
    [property: MaxLength(20)]
    string Phone);
