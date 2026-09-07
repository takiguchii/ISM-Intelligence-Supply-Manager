namespace ISM.Application.DTOs;

public sealed record ProductResponse(
    int Id,
    int RestaurantId,
    string Name,
    string Unit,
    decimal CurrentQuantity,
    decimal MinimumQuantity,
    decimal AverageCost,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
