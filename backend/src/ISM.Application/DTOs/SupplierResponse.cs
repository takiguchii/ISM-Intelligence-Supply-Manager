namespace ISM.Application.DTOs;

public sealed record SupplierResponse(
    int Id,
    int RestaurantId,
    string Name,
    string Category,
    string? Description,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
