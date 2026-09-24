namespace ISM.Application.DTOs;

public sealed record MarginAlertResponse(
    int DishId,
    string DishName,
    decimal PreviousCmvPercent,
    decimal CurrentCmvPercent,
    decimal TargetMarginPercent,
    decimal SuggestedPrice,
    string Message);
