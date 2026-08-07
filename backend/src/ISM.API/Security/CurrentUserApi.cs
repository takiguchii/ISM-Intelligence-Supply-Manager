using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ISM.Application.Security;
using Microsoft.AspNetCore.Http;

namespace ISM.API.Security;

public sealed class CurrentUserApi : ICurrentUser
{
    public int UserId { get; }
    public string? Email { get; }
    public string Role { get; }
    public int? RestaurantId { get; }
    public bool IsSuperAdmin =>
        string.Equals(Role, IsmRoles.Admin, StringComparison.OrdinalIgnoreCase) && !RestaurantId.HasValue;
    public bool IsManagerOrAbove =>
        string.Equals(Role, IsmRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Role, IsmRoles.Manager, StringComparison.OrdinalIgnoreCase);

    public CurrentUserApi(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            Role = IsmRoles.Waiter;
            return;
        }

        var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idClaim, out var uid)) UserId = uid;

        Email = user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? user.FindFirstValue(ClaimTypes.Email);
        Role = IsmRoles.Normalize(user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue(JwtRegisteredClaimNames.Name));

        var restClaim = user.FindFirstValue("restaurantId");
        if (!string.IsNullOrWhiteSpace(restClaim) && int.TryParse(restClaim, out var rid))
            RestaurantId = rid;
    }
}
