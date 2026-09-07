namespace ISM.Application.DTOs;

public sealed record RegisterTenantResponse(
    RestaurantTenantResponse Restaurant,
    ManagerTenantResponse Manager,
    string AccessToken,
    DateTime TokenExpiresAt);

public sealed record RestaurantTenantResponse(
    int Id,
    string Name,
    string Cnpj);

public sealed record ManagerTenantResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    int RestaurantId);
