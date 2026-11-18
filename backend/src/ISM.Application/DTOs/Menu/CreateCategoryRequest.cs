using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record CreateCategoryRequest(
    [Required]
    int RestaurantId,
    [Required]
    [MaxLength(80)]
    string Name,
    bool IsActive,
    [Range(0, int.MaxValue)]
    int DisplayOrder);