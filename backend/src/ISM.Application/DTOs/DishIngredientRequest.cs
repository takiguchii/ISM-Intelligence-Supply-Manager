using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record DishIngredientRequest(
    [property: Required]
    int ProductId,
    [property: Range(0.001, double.MaxValue)]
    decimal Quantity);