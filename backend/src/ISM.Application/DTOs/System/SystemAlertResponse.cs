using ISM.Domain.Modules.System.Enums;

namespace ISM.Application.DTOs;

public sealed record SystemAlertResponse(
    int Id,
    int RestaurantId,
    SystemAlertType AlertType,
    AlertSeverity Severity,
    string Title,
    string Message,
    string? ReferenceEntityType,
    int? ReferenceEntityId,
    string? PayloadSerializedJson,
    bool IsRead,
    bool IsDismissed,
    DateTime GeneratedAtUtc,
    DateTime? ReadAtUtc,
    DateTime? DismissedAtUtc,
    int? DismissedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
