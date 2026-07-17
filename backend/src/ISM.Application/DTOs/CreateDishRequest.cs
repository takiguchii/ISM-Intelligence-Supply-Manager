using System.ComponentModel.DataAnnotations;
using ISM.Application.DTOs;

namespace ISM.Application.DTOs;

public sealed record CreateDishRequest(
    [property: Required]
    int RestaurantId,
    [property: Required]
    int CategoryId,
    [property: Required]
    [property: MaxLength(80)]
    string Name,
    [property: MaxLength(255)]
    string Description,
    [property: Range(0, double.MaxValue)]
    decimal Price,
    [property: Range(0, double.MaxValue)]
    decimal Cost,
    bool IsAtive,
    bool Highlight,
    [property: MaxLength(255)]
    string? UrlImage,
    [property: Range(0, int.MaxValue)]
    int DisplayOrder,
    IReadOnlyCollection<DishIngredientRequest> Ingredients);