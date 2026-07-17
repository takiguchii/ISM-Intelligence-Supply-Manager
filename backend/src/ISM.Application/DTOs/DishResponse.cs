namespace ISM.Application.DTOs;

public sealed record DishResponse(
    int Id,
    int RestaurantId,
    int CategoryId,
    string Name,
    string Description,
    decimal Price,
    decimal Cost,
    bool IsAtive,
    bool Highlight,
    string? UrlImage,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<DishIngredientResponse> Ingredients);