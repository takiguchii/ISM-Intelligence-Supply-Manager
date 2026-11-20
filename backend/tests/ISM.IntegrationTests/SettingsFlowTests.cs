using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ISM.Application.DTOs;
using ISM.Domain.Modules.System.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OtpNet;

namespace ISM.IntegrationTests;

public sealed class SettingsFlowTests
{
    [Fact]
    public async Task Menu_AllowsCreatingCategoryAndDishWithIngredients()
    {
        await using var factory = new SettingsFactory();
        using var manager = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<IsmDbContext>(), "Settings123!");
        await Login(manager, "gerente@ism.com.br", "Settings123!");
        var user = (await manager.GetFromJsonAsync<UserDto>("/api/auth/me"))!;
        var restaurant = user.RestaurantId!.Value;
        (await manager.PostAsJsonAsync("/api/menu/categories", new CreateCategoryRequest(restaurant, "", true, 0)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var create = await manager.PostAsJsonAsync("/api/menu/categories", new CreateCategoryRequest(restaurant, "Pratos demo", true, 0));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = (await create.Content.ReadFromJsonAsync<CategoryResponse>())!;
        int product;
        using (var scope = factory.Services.CreateScope())
            product = await scope.ServiceProvider.GetRequiredService<IsmDbContext>().Products.IgnoreQueryFilters()
                .Where(item => item.RestaurantId == restaurant).Select(item => item.Id).FirstAsync();

        var dish = new CreateDishRequest(restaurant, category.Id, "Prato demo", "", 20m, 0m, true, false, null, 0, [new(product, 0.1m)]);
        var result = await manager.PostAsJsonAsync("/api/menu/dishes", dish);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        (await result.Content.ReadFromJsonAsync<DishResponse>())!.Ingredients.Should().ContainSingle(item => item.ProductId == product);
        var invalid = dish with { Ingredients = [new(product, -1m)] };
        (await manager.PostAsJsonAsync("/api/menu/dishes", invalid)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Profile_IsPrivateAndPersistsNameAndPhoto()
    {
        await using var factory = new SettingsFactory();
        using var manager = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<IsmDbContext>(), "Settings123!");

        const string photo = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/aXcAAAAASUVORK5CYII=";
        (await manager.PutAsJsonAsync("/api/me/profile", new UpdateMyProfileRequest("Perfil", photo)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var chef = factory.CreateClient();
        await Login(manager, "gerente@ism.com.br", "Settings123!");
        await Login(chef, "chef@ism.com.br", "Settings123!");
        var original = (await chef.GetFromJsonAsync<UserDto>("/api/auth/me"))!;
        var changed = await chef.PutAsJsonAsync("/api/me/profile", new { name = "  Chef Demo  ", avatarDataUrl = photo, role = "Admin", restaurantId = 999 });
        changed.EnsureSuccessStatusCode();
        var after = (await chef.GetFromJsonAsync<UserDto>("/api/auth/me"))!;
        after.Name.Should().Be("Chef Demo");
        after.AvatarDataUrl.Should().Be(photo);
        after.Role.Should().Be(original.Role);
        after.RestaurantId.Should().Be(original.RestaurantId);
        (await manager.GetFromJsonAsync<UserDto>("/api/auth/me"))!.AvatarDataUrl.Should().BeNull();
        (await chef.PutAsJsonAsync("/api/me/profile", new UpdateMyProfileRequest("Chef Demo", "data:image/png;base64,PHN2Zz4=")))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await chef.GetFromJsonAsync<UserDto>("/api/auth/me"))!.AvatarDataUrl.Should().Be(photo);
        (await chef.PutAsJsonAsync("/api/me/profile", new UpdateMyProfileRequest("Chef Demo", null))).EnsureSuccessStatusCode();
        (await chef.GetFromJsonAsync<UserDto>("/api/auth/me"))!.AvatarDataUrl.Should().BeNull();
    }

    [Fact]
    public async Task DismissAll_PreservesAuditAndOtherRestaurants()
    {
        await using var factory = new SettingsFactory();
        using var manager = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        await DbSeeder.SeedAsync(db, "Settings123!");
        await Login(manager, "gerente@ism.com.br", "Settings123!");
        var user = (await manager.GetFromJsonAsync<UserDto>("/api/auth/me"))!;
        using var chef = factory.CreateClient();
        await Login(chef, "chef@ism.com.br", "Settings123!");
        var foreignRestaurant = user.RestaurantId!.Value + 1000;
        var now = DateTime.UtcNow;
        db.Set<SystemAlert>().AddRange(
            new SystemAlert { RestaurantId = user.RestaurantId.Value, Title = "Lido", Message = "Teste", IsRead = true, GeneratedAtUtc = now },
            new SystemAlert { RestaurantId = user.RestaurantId.Value, Title = "Não lido", Message = "Teste", GeneratedAtUtc = now },
            new SystemAlert { RestaurantId = foreignRestaurant, Title = "Outro restaurante", Message = "Teste", GeneratedAtUtc = now });
        await db.SaveChangesAsync();
        (await chef.PatchAsync("/api/system/alerts/dismiss-all", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await manager.PatchAsync("/api/system/alerts/dismiss-all", null)).EnsureSuccessStatusCode();
        var all = await db.Set<SystemAlert>().IgnoreQueryFilters().AsNoTracking().ToListAsync();
        all.Where(alert => alert.RestaurantId == user.RestaurantId).Should().OnlyContain(alert =>
            alert.IsDismissed && alert.DismissedAtUtc.HasValue && alert.DismissedByUserId == user.Id);
        all.Single(alert => alert.RestaurantId == foreignRestaurant).IsDismissed.Should().BeFalse();
        using var again = await manager.PatchAsync("/api/system/alerts/dismiss-all", null);
        again.EnsureSuccessStatusCode();
        (await again.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["count"].Should().Be(0);
    }

    [Fact]
    public async Task Settings_Persist_EnforcePermissions_AndProtectAccount()
    {
        await using var factory = new SettingsFactory();
        using var manager = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<IsmDbContext>(), "Settings123!");
        await Login(manager, "gerente@ism.com.br", "Settings123!");
        var profile = (await manager.GetFromJsonAsync<RestaurantDto>("/api/me/restaurant/profile"))!;
        profile.Address = "Rua de teste, 42";
        profile.Phone = "11999999999";
        profile.TimeZone = "America/Manaus";
        profile.Currency = "USD";
        profile.Language = "en-US";
        profile.OpeningHours = "Segunda a sexta, 11h às 23h";
        profile.ServiceFeePercent = 10;
        profile.DeliveryFee = 7.5m;
        (await manager.PutAsJsonAsync("/api/me/restaurant/profile", profile)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var saved = (await manager.GetFromJsonAsync<RestaurantDto>("/api/me/restaurant/profile"))!;
        saved.Address.Should().Be(profile.Address);
        saved.TimeZone.Should().Be("America/Manaus");
        saved.DeliveryFee.Should().Be(7.5m);
        (await manager.PutAsJsonAsync("/api/me/restaurant/profile", profile)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        profile.TimeZone = "Invalid/Zone";
        (await manager.PutAsJsonAsync("/api/me/restaurant/profile", profile)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await manager.GetAsync($"/api/restaurants/{profile.Id}/profile")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var appearance = new AppearancePreferences { Theme = "light", Density = "compact", FontScale = 115, ReduceMotion = true, HighContrast = true };
        (await manager.PutAsJsonAsync("/api/me/appearance", appearance)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var savedAppearance = (await manager.GetFromJsonAsync<AppearancePreferences>("/api/me/appearance"))!;
        savedAppearance.Theme.Should().Be("light");
        savedAppearance.ReduceMotion.Should().BeTrue();
        appearance.FontScale = 200;
        (await manager.PutAsJsonAsync("/api/me/appearance", appearance)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await manager.PutAsJsonAsync("/api/me/notification-preferences", new
        { stock = true, suppliers = true, menu = true, whatsAppEnabled = true })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await manager.PutAsJsonAsync("/api/me/notification-preferences", new
        { stock = true, whatsAppPhone = "11999999999" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await manager.GetAsync("/api/notifications/whatsapp/status")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await manager.PostAsJsonAsync("/api/me/notifications/test", new { channel = "email" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await manager.GetAsync("/api/me/notifications/deliveries")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var chef = factory.CreateClient();
        await Login(chef, "chef@ism.com.br", "Settings123!");
        (await chef.PutAsJsonAsync("/api/me/restaurant/profile", saved)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await manager.PutAsJsonAsync("/api/me/notification-preferences", new NotificationPreferences(false, true, false))).EnsureSuccessStatusCode();
        (await manager.GetFromJsonAsync<NotificationPreferences>("/api/me/notification-preferences"))!.Stock.Should().BeFalse();
        (await chef.GetFromJsonAsync<NotificationPreferences>("/api/me/notification-preferences"))!.Stock.Should().BeTrue();

        var sessions = (await manager.GetFromJsonAsync<List<SessionView>>("/api/auth/sessions"))!;
        sessions.Should().ContainSingle(s => s.IsCurrent);
        var sessionId = sessions.Single(s => s.IsCurrent).Id;
        (await chef.DeleteAsync($"/api/auth/sessions/{sessionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await manager.DeleteAsync($"/api/auth/sessions/{sessionId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await manager.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await Login(manager, "gerente@ism.com.br", "Settings123!");

        var subscription = (await manager.GetFromJsonAsync<System.Text.Json.Nodes.JsonObject>("/api/me/restaurant/subscription"))!;
        var plan = subscription["subscription"]!["plans"]!.AsArray().Single(p => p!["name"]!.GetValue<string>() == "Enterprise")!;
        var planId = plan["id"]!.GetValue<int>();
        (await manager.PostAsJsonAsync("/api/me/restaurant/subscription/plan", new { planId })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        subscription = (await manager.GetFromJsonAsync<System.Text.Json.Nodes.JsonObject>("/api/me/restaurant/subscription"))!;
        subscription["subscription"]!["invoices"]!.AsArray().Should().ContainSingle();
        (await chef.PostAsJsonAsync("/api/me/restaurant/subscription/plan", new { planId })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var account = factory.CreateClient();
        var response = await manager.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Conta de teste", email = "settings-account@ism.test", password = "Original123!", role = "Chef", restaurantId = saved.Id
        });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        account.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        (await account.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Wrong123!", newPassword = "Updated123!" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await account.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Original123!", newPassword = "Updated123!" })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await account.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await account.PostAsJsonAsync("/api/auth/login", new { email = "settings-account@ism.test", password = "Original123!" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await Login(account, "settings-account@ism.test", "Updated123!");
        var setupResponse = await account.PostAsJsonAsync("/api/auth/two-factor/setup", new { password = "Updated123!" });
        setupResponse.EnsureSuccessStatusCode();
        var setup = (await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetup>())!;
        var code = new Totp(Base32Encoding.ToBytes(setup.Secret)).ComputeTotp();
        var enable = await account.PostAsJsonAsync("/api/auth/two-factor/enable", new { code });
        enable.EnsureSuccessStatusCode();
        var recovery = (await enable.Content.ReadFromJsonAsync<RecoveryResponse>())!.RecoveryCodes;
        recovery.Should().HaveCount(8);
        (await account.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await account.PostAsJsonAsync("/api/auth/login", new { email = "settings-account@ism.test", password = "Updated123!" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await account.PostAsJsonAsync("/api/auth/login", new { email = "settings-account@ism.test", password = "Updated123!", twoFactorCode = code })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await Login(account, "settings-account@ism.test", "Updated123!", recovery[0]);
        (await account.PostAsJsonAsync("/api/auth/login", new { email = "settings-account@ism.test", password = "Updated123!", twoFactorCode = recovery[0] })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await account.PostAsJsonAsync("/api/auth/two-factor/disable", new { password = "Updated123!", code = recovery[1] })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await Login(account, "settings-account@ism.test", "Updated123!");
    }

    private static async Task Login(HttpClient client, string email, string password, string? code = null)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password, twoFactorCode = code });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }
    private sealed record SessionView(string Id, bool IsCurrent);
    private sealed record RecoveryResponse(string[] RecoveryCodes);

    private sealed class SettingsFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:SecretKey", "settings-test-secret-key-at-least-32-bytes");
            builder.UseSetting("Jwt:Issuer", "ism-settings-tests");
            builder.UseSetting("Jwt:Audience", "ism-settings-tests");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Jwt:SecretKey"] = "settings-test-secret-key-at-least-32-bytes",
                ["Jwt:Issuer"] = "ism-settings-tests", ["Jwt:Audience"] = "ism-settings-tests",
                ["Billing:DemoEnabled"] = "true"
            }));
            var databaseName = "settings-" + Guid.NewGuid();
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<IsmDbContext>>();
                services.AddDbContext<IsmDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        }
    }
}
