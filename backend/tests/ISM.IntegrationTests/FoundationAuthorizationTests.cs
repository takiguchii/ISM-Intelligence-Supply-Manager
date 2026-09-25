using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using ISM.Application.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ISM.Infrastructure.Data.Context;

namespace ISM.IntegrationTests;

public sealed class FoundationAuthorizationTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FoundationAuthorizationTests(FoundationWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task UsersEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        (await _client.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(IsmRoles.Waiter)]
    [InlineData(IsmRoles.Chef)]
    [InlineData(IsmRoles.Manager)]
    public async Task UsersEndpoint_NonAdminRestaurantRole_ReturnsForbidden(string role)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(role, 10));

        (await _client.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RestaurantAdmin_CanManageUsers_ButCannotAccessPlatformEndpoint()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Admin, 10));
        (await _client.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetAsync("/api/restaurants")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformSuperAdmin_CanAccessPlatform_ButNotTenantEndpoint()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Admin));
        (await _client.GetAsync("/api/restaurants")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetAsync("/api/stock/products")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

public sealed class FoundationWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string Secret = "integration-test-secret-key-with-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:SecretKey", Secret);
        builder.UseSetting("Jwt:Issuer", "ism-integration-tests");
        builder.UseSetting("Jwt:Audience", "ism-integration-tests");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "InMemory",
            ["Jwt:SecretKey"] = Secret,
            ["Jwt:Issuer"] = "ism-integration-tests",
            ["Jwt:Audience"] = "ism-integration-tests"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IsmDbContext>>();
            services.AddDbContext<IsmDbContext>(options => options.UseInMemoryDatabase("foundation-integration-tests"));
        });
    }

    public static string Token(string role, int? restaurantId = null)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, "1"), new(ClaimTypes.Role, role) };
        if (restaurantId.HasValue) claims.Add(new Claim("restaurantId", restaurantId.Value.ToString()));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("ism-integration-tests", "ism-integration-tests", claims,
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials));
    }
}
