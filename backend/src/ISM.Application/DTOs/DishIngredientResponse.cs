namespace ISM.Application.DTOs;

public sealed record DishIngredientResponse(
    int ProductId,
    decimal Quantity);