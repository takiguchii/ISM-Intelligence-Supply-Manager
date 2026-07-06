namespace ISM.Application.DTOs;

public sealed record ProductResponse(
    int Id,
    string Name,
    string Unit,
    decimal CurrentQuantity,
    decimal MinimumQuantity,
    decimal AverageCost,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
