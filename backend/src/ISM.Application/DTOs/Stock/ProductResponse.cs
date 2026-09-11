namespace ISM.Application.DTOs;

public sealed record ProductResponse(
    int Id,
    int RestaurantId,
    string Name,
    string Unit,
    decimal CurrentQuantity,
    decimal MinimumQuantity,
    decimal MaximumQuantity,
    decimal ReorderPoint,
    decimal AverageCost,
    decimal MovingAverageConsumption,
    DateTime? LastConsumptionRecalculatedAtUtc,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
