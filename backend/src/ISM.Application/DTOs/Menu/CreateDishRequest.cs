using System.ComponentModel.DataAnnotations;
using ISM.Application.DTOs;

namespace ISM.Application.DTOs;

public sealed record CreateDishRequest(
    [Required]
    int RestaurantId,
    [Required]
    int CategoryId,
    [Required]
    [MaxLength(80)]
    string Name,
    [MaxLength(255)]
    string Description,
    [Range(0, double.MaxValue)]
    decimal Price,
    [Range(0, double.MaxValue)]
    decimal Cost,
    bool IsActive,
    bool Highlight,
    [MaxLength(255)]
    string? UrlImage,
    [Range(0, int.MaxValue)]
    int DisplayOrder,
    IReadOnlyCollection<DishIngredientRequest> Ingredients);