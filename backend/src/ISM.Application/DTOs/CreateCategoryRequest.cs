using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record CreateCategoryRequest(
    [property: Required]
    int RestaurantId,
    [property: Required]
    [property: MaxLength(80)]
    string Name,
    bool IsActive,
    [property: Range(0, int.MaxValue)]
    int DisplayOrder);