namespace ISM.Application.DTOs;

public sealed record RegisterTenantResponse(
    int RestaurantId,
    string RestaurantName,
    string RestaurantCnpj,
    int ManagerUserId,
    string ManagerName,
    string ManagerEmail,
    string AccessToken,
    DateTime TokenExpiresAt);
