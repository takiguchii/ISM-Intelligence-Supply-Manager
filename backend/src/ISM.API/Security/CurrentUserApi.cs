using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ISM.Application.Security;

namespace ISM.API.Security;

public sealed class CurrentUserApi(
    IHttpContextAccessor httpContextAccessor,
    IBackgroundTenantContext backgroundTenantContext) : ICurrentUser
{
    // O contexto pode ser resolvido durante a validação do JWT, antes de o
    // middleware atribuir HttpContext.User. Leia as claims na hora do uso.
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
    private bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public int UserId => int.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? Principal?.FindFirstValue(ClaimTypes.Email);
    public string Role => IsAuthenticated
        ? IsmRoles.Normalize(Principal?.FindFirstValue(ClaimTypes.Role))
        : IsmRoles.Admin;
    public int? RestaurantId => IsAuthenticated
        ? (int.TryParse(Principal?.FindFirstValue("restaurantId"), out var id) ? id : null)
        : backgroundTenantContext.RestaurantId;
    public bool IsSuperAdmin => Role == IsmRoles.Admin && !RestaurantId.HasValue;
    public bool IsManagerOrAbove => Role is IsmRoles.Admin or IsmRoles.Manager;
}
