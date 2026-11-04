using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.IntegrationTests;

public sealed class EmployeeLifecycleTests : IClassFixture<FoundationWebApplicationFactory>
{
    private const int TenantId = 9704;
    private const int PlanId = 9704;
    private readonly FoundationWebApplicationFactory _factory;

    public EmployeeLifecycleTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Manager_CanCreateAndAuthenticateWaiterAndChef_WithTenantIsolation()
    {
        var waiterEmail = $"waiter-{Guid.NewGuid():N}@ism.test";
        var chefEmail = $"chef-{Guid.NewGuid():N}@ism.test";
        const string password = "TesteSeguro123!";
        await EnsureTenantAsync();

        using var manager = _factory.CreateClient();
        manager.DefaultRequestHeaders.Authorization = Bearer(IsmRoles.Manager, TenantId);

        var waiter = await RegisterAsync(manager, "Waiter Teste", waiterEmail, password, IsmRoles.Waiter);
        var chef = await RegisterAsync(manager, "Chef Teste", chefEmail, password, IsmRoles.Chef);

        waiter.User.Role.Should().Be(IsmRoles.Waiter);
        waiter.User.RestaurantId.Should().Be(TenantId);
        chef.User.Role.Should().Be(IsmRoles.Chef);
        chef.User.RestaurantId.Should().Be(TenantId);

        var employees = await manager.GetFromJsonAsync<List<UserDetailsDto>>($"/api/users?restaurantId={TenantId}");
        employees.Should().Contain(user => user.Email == waiterEmail && user.Role == IsmRoles.Waiter);
        employees.Should().Contain(user => user.Email == chefEmail && user.Role == IsmRoles.Chef);

        using var waiterClient = _factory.CreateClient();
        waiterClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", waiter.Token);
        var waiterMe = await waiterClient.GetFromJsonAsync<UserDto>("/api/auth/me");
        waiterMe!.Role.Should().Be(IsmRoles.Waiter);
        waiterMe.RestaurantId.Should().Be(TenantId);
        (await waiterClient.GetAsync("/api/finance")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await waiterClient.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var chefClient = _factory.CreateClient();
        chefClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", chef.Token);
        var chefMe = await chefClient.GetFromJsonAsync<UserDto>("/api/auth/me");
        chefMe!.Role.Should().Be(IsmRoles.Chef);
        chefMe.RestaurantId.Should().Be(TenantId);
        (await chefClient.GetAsync("/api/dashboard/metrics")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var loginClient = _factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = waiterEmail, Password = password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await login.Content.ReadFromJsonAsync<AuthResponse>())!.User.Role.Should().Be(IsmRoles.Waiter);

        var wrongPassword = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = waiterEmail, Password = "SenhaErrada123!" });
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var unknownUser = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = $"missing-{Guid.NewGuid():N}@ism.test", Password = password });
        unknownUser.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var invalidTokenClient = _factory.CreateClient();
        invalidTokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");
        (await invalidTokenClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var expiredTokenClient = _factory.CreateClient();
        expiredTokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.ExpiredToken(IsmRoles.Waiter, TenantId));
        (await expiredTokenClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var duplicate = await RegisterAsyncExpecting(manager, "Duplicado", waiterEmail, password, IsmRoles.Waiter);
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidRole = await RegisterAsyncExpecting(manager, "Cargo inválido", $"invalid-{Guid.NewGuid():N}@ism.test", password, "InvalidRole");
        invalidRole.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var incomplete = await manager.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = $"incomplete-{Guid.NewGuid():N}@ism.test",
            Password = password,
            Role = IsmRoles.Waiter,
            RestaurantId = TenantId
        });
        incomplete.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var otherRestaurant = await RegisterAsyncExpecting(manager, "Outro restaurante", $"other-{Guid.NewGuid():N}@ism.test", password, IsmRoles.Waiter, TenantId + 1);
        otherRestaurant.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var waiterRegistrationClient = _factory.CreateClient();
        waiterRegistrationClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", waiter.Token);
        var unauthorizedRegistration = await waiterRegistrationClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Name = "Cadastro não autorizado",
            Email = $"blocked-{Guid.NewGuid():N}@ism.test",
            Password = password,
            Role = IsmRoles.Chef,
            RestaurantId = TenantId
        });
        unauthorizedRegistration.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<AuthResponse> RegisterAsync(HttpClient client, string name, string email, string password, string role)
    {
        var response = await RegisterAsyncExpecting(client, name, email, password, role);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static Task<HttpResponseMessage> RegisterAsyncExpecting(HttpClient client, string name, string email, string password, string role, int? restaurantId = null) =>
        client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password,
            Role = role,
            RestaurantId = restaurantId ?? TenantId
        });

    private async Task EnsureTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        var plan = await db.Plans.FindAsync(PlanId);
        if (plan is null)
        {
            plan = new Plan
            {
                Id = PlanId,
                Name = "Pro - Task 4",
                MaxUsers = 10,
                MaxDishes = 100,
                MaxProducts = 100,
                MaxCategories = 20,
                MonthlyPrice = 149.90m,
                IsActive = true
            };
            db.Plans.Add(plan);
        }

        if (await db.Restaurants.FindAsync(TenantId) is null)
        {
            db.Restaurants.Add(new Restaurant
            {
                Id = TenantId,
                Name = "Restaurante de Teste Task 4",
                CNPJ = "97040000000104",
                PlanId = plan.Id,
                IsPlanActive = true,
                Created = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private static AuthenticationHeaderValue Bearer(string role, int restaurantId) =>
        new("Bearer", FoundationWebApplicationFactory.Token(role, restaurantId));
}
