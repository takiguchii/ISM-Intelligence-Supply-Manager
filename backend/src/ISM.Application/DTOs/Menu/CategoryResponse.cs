namespace ISM.Application.DTOs;

public sealed record CategoryResponse(
    int Id,
    int RestaurantId,
    string Name,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);