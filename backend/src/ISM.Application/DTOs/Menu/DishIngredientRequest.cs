using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed record DishIngredientRequest(
    [Required]
    int ProductId,
    [Range(0.001, double.MaxValue)]
    decimal Quantity);