using System.Security.Claims;
using FluentAssertions;
using ISM.API.Extensions;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.UnitTests;

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public async Task RestaurantAdmin_ShouldBeForbiddenFromPlatformPolicy()
    {
        var result = await AuthorizeAsync(Principal(IsmRoles.Admin, 10), IsmPolicies.SuperAdminOnly);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task PlatformSuperAdmin_ShouldBeAllowedOnlyOnPlatformPolicy()
    {
        var platformResult = await AuthorizeAsync(Principal(IsmRoles.Admin), IsmPolicies.SuperAdminOnly);
        var tenantResult = await AuthorizeAsync(Principal(IsmRoles.Admin), IsmPolicies.RestaurantAnyUser);

        platformResult.Succeeded.Should().BeTrue();
        tenantResult.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RestaurantAdmin_ShouldBeAllowedOnOwnTenantPolicy()
    {
        var result = await AuthorizeAsync(Principal(IsmRoles.Admin, 10), IsmPolicies.RestaurantAnyUser);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task BothPlatformSuperAdminAndRestaurantAdmin_ShouldBeAllowedOnUserManagementPolicy()
    {
        var superAdminResult = await AuthorizeAsync(Principal(IsmRoles.Admin), IsmPolicies.UserManagement);
        var restaurantAdminResult = await AuthorizeAsync(Principal(IsmRoles.Admin, 10), IsmPolicies.UserManagement);
        var managerResult = await AuthorizeAsync(Principal(IsmRoles.Manager, 10), IsmPolicies.UserManagement);

        superAdminResult.Succeeded.Should().BeTrue();
        restaurantAdminResult.Succeeded.Should().BeTrue();
        managerResult.Succeeded.Should().BeFalse();
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal principal, string policy)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "InMemory",
            ["Jwt:SecretKey"] = "test-secret-key-with-sufficient-length-12345",
            ["Jwt:Issuer"] = "ism-tests",
            ["Jwt:Audience"] = "ism-tests"
        }).Build();
        services.AddApiServices(configuration);
        await using var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, policy);
    }

    private static ClaimsPrincipal Principal(string role, int? restaurantId = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (restaurantId.HasValue)
            claims.Add(new Claim("restaurantId", restaurantId.Value.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
