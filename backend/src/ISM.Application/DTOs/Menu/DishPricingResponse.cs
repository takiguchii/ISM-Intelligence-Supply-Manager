namespace ISM.Application.DTOs;

public sealed record DishPricingResponse (
    int DishId,
    string DishName,
    decimal CurrentCost,
    decimal CurrentCmvPercent,
    decimal TargetMarginPercent,
    MarginAlertResponse? Alert);