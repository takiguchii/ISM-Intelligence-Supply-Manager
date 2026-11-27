using FluentAssertions;
using ISM.API.Extensions;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Interfaces.Notifications;
using ISM.Application.Interfaces.Telegram;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Orders;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Headers;

namespace ISM.IntegrationTests;

public sealed class TelegramIntegrationTests
{
    [Fact] public async Task HttpEndpoints_RejectAnonymousAndOperationalRoles()
    {
        using var root = new FoundationWebApplicationFactory();
        var database = Guid.NewGuid().ToString();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IsmDbContext>>();
            services.AddDbContext<IsmDbContext>(o => o.UseInMemoryDatabase(database));
        }));
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/me/telegram")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        foreach (var role in new[] { "Chef", "Waiter", "Cashier", "Admin" })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                FoundationWebApplicationFactory.Token(role, role == "Admin" ? null : 10));
            (await client.GetAsync("/api/me/telegram")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await client.PostAsync("/api/me/telegram/link", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await client.DeleteAsync("/api/me/telegram")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan time) => now += time;
    }
    private sealed class Transport : ITelegramTransport
    {
        public List<(long Chat, string Text)> Sent { get; } = [];
        public Task SendAsync(long chatId, string text, CancellationToken ct) { Sent.Add((chatId, text)); return Task.CompletedTask; }
        public Task ConfigureAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<TelegramIncoming>> GetUpdatesAsync(long offset, CancellationToken ct) => Task.FromResult<IReadOnlyList<TelegramIncoming>>([]);
    }
    private sealed class Llm : ILlmTextProvider
    {
        public Task<LlmTextResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default) => Task.FromResult(new LlmTextResult(true, "Resposta de teste do assistente", null));
        public Task<LlmTextResult> CompleteWithImageAsync(string a, string b, byte[] c, string d, CancellationToken ct = default) => CompleteAsync(a, b, ct);
        public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<LlmProviderStatus> GetStatusAsync(CancellationToken ct = default) => Task.FromResult(new LlmProviderStatus(true, true, "Teste", "Teste", null));
    }
    private sealed class Fixture : IDisposable
    {
        public ServiceProvider Services { get; }
        public Clock Clock { get; } = new();
        public Transport Transport { get; } = new();
        public Fixture()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Database:Provider"] = "InMemory", ["Telegram:Enabled"] = "true",
                ["Telegram:BotToken"] = "12345:test-only", ["Telegram:BotUsername"] = "test_ism_bot",
                ["Telegram:IsmUrl"] = "http://localhost:3000"
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging(); services.AddSingleton<IConfiguration>(config); services.AddApiServices(config);
            services.RemoveAll<DbContextOptions<IsmDbContext>>();
            // O nome precisa ser o mesmo entre os escopos deste teste.
            var name = Guid.NewGuid().ToString();
            services.RemoveAll<DbContextOptions<IsmDbContext>>();
            services.AddDbContext<IsmDbContext>(o => o.UseInMemoryDatabase(name));
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<ITelegramTransport>(Transport);
            services.AddScoped<ILlmTextProvider, Llm>();
            Services = services.BuildServiceProvider();
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            db.Restaurants.AddRange(new Restaurant { Id = 10, Name = "Restaurante A", CNPJ = "A" }, new Restaurant { Id = 20, Name = "Restaurante B", CNPJ = "B" });
            db.Users.AddRange(new User { Id = 100, RestaurantId = 10, Role = "Manager", Name = "Gerente A", Email = "a@test.local" },
                new User { Id = 200, RestaurantId = 20, Role = "Manager", Name = "Gerente B", Email = "b@test.local" });
            db.Dishes.AddRange(new Dish { Id = 1, RestaurantId = 10, Name = "Prato A", Price = 25, IsActive = true }, new Dish { Id = 2, RestaurantId = 20, Name = "Segredo B", Price = 80, IsActive = true });
            db.Orders.Add(new Order { Id = 20, RestaurantId = 20, TableReference = "Mesa secreta", RequestId = Guid.NewGuid() });
            db.SaveChanges();
        }
        public async Task<string> IssueAsync()
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TelegramUserContext>().Set(100, 10, "Manager");
            var link = await scope.ServiceProvider.GetRequiredService<ITelegramService>().CreateLinkAsync(default, telegramUserId: 123);
            return link.Url.Split("start=")[1];
        }
        public async Task<string?> MessageAsync(string text, long update = 1, long chat = 123, string type = "private", long? sender = null)
        {
            using var scope = Services.CreateScope();
            Clock.Advance(TimeSpan.FromSeconds(3));
            return await scope.ServiceProvider.GetRequiredService<ITelegramService>().HandleAsync(new(update, chat, sender ?? chat, type, text), default);
        }
        public async Task ConnectAsync() => (await MessageAsync("/start " + await IssueAsync())).Should().Contain("Conta ISM conectada");
        public void Dispose() => Services.Dispose();
    }

    [Fact] public async Task Linking_StoresHash_ConsumesOnce_AndStartsWithAlertsOff()
    {
        using var f = new Fixture(); var code = await f.IssueAsync();
        using (var scope = f.Services.CreateScope())
        {
            var link = await scope.ServiceProvider.GetRequiredService<ITelegramRepository>().ForUserAsync(100, default);
            link!.CodeHash.Should().NotBe(code); link.CodeHash.Should().HaveLength(64);
        }
        (await f.MessageAsync("/start " + code)).Should().Contain("Conta ISM conectada");
        (await f.MessageAsync("/start " + code, 2, 999)).Should().Contain("expirou ou não é válido");
        using var check = f.Services.CreateScope();
        var connected = await check.ServiceProvider.GetRequiredService<ITelegramRepository>().ForUserAsync(100, default);
        connected!.ChatId.Should().Be(123); connected.AlertsEnabled.Should().BeFalse(); connected.CodeHash.Should().BeNull();
    }
    [Fact] public async Task ExpiredLink_AndSupersededLink_CannotConnect()
    {
        using var f = new Fixture(); var old = await f.IssueAsync(); var next = await f.IssueAsync();
        (await f.MessageAsync("/start " + old)).Should().Contain("expirou ou não é válido");
        f.Clock.Advance(TimeSpan.FromMinutes(6));
        (await f.MessageAsync("/start " + next, 2)).Should().Contain("expirou ou não é válido");
    }
    [Fact] public async Task ConcurrentLinkChanges_OnlyFirstVersionCanBeSaved()
    {
        using var f = new Fixture(); await f.IssueAsync();
        using var firstScope = f.Services.CreateScope(); using var secondScope = f.Services.CreateScope();
        var firstRepo = firstScope.ServiceProvider.GetRequiredService<ITelegramRepository>();
        var secondRepo = secondScope.ServiceProvider.GetRequiredService<ITelegramRepository>();
        var first = (await firstRepo.ForUserAsync(100, default))!;
        var second = (await secondRepo.ForUserAsync(100, default))!;
        first.ChatId = 123; first.CodeHash = null;
        second.ChatId = 456; second.CodeHash = null;
        await firstRepo.SaveAsync(first, default);
        var stale = () => secondRepo.SaveAsync(second, default);
        await stale.Should().ThrowAsync<InvalidOperationException>();
        using var check = f.Services.CreateScope();
        (await check.ServiceProvider.GetRequiredService<ITelegramRepository>().ForUserAsync(100, default))!.ChatId.Should().Be(123);
    }
    [Fact] public async Task Groups_AndSpoofedSender_AreIgnored()
    {
        using var f = new Fixture(); var code = await f.IssueAsync();
        (await f.MessageAsync("/start " + code, type: "group")).Should().BeNull();
        (await f.MessageAsync("/start " + code, sender: 456)).Should().BeNull();
        (await f.MessageAsync("/start " + code)).Should().Contain("conectada");
    }
    [Fact] public async Task Commands_IsolateRestaurant_AndDuplicateUpdatesAreIgnored()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        var menu = await f.MessageAsync("/cardapio", 2);
        menu.Should().Contain("Prato A").And.NotContain("Segredo B");
        (await f.MessageAsync("/pedido 20", 3)).Should().Contain("não encontrado neste restaurante");
        (await f.MessageAsync("/cardapio", 3)).Should().BeNull();
    }
    [Fact] public async Task RevokedUser_CannotRead_AndConnectionIsCleared()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            (await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == 100)).IsActive = false; await db.SaveChangesAsync();
        }
        (await f.MessageAsync("/cardapio", 2)).Should().Contain("acesso ao restaurante mudou");
        (await f.MessageAsync("/cardapio", 3)).Should().Contain("Somente o administrador");
    }
    [Fact] public async Task Conversation_PersistsThreadAndResponses_AndCanStartAgain()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        (await f.MessageAsync("Olá, como você pode me ajudar?", 2)).Should().Contain("Resposta de teste");
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            var thread = await db.AssistantThreads.IgnoreQueryFilters().SingleAsync();
            thread.UserId.Should().Be(100); thread.RestaurantId.Should().Be(10);
            (await db.AssistantMessages.IgnoreQueryFilters().CountAsync(m => m.Role == "user")).Should().Be(1);
            (await db.AssistantMessages.IgnoreQueryFilters().CountAsync(m => m.Content == "Resposta de teste do assistente")).Should().Be(1);
        }
        (await f.MessageAsync("/nova", 3)).Should().Contain("Conversa reiniciada");
    }
    [Fact] public async Task Alerts_UseOptIn_AndDisconnectCancelsQueuedDeliveries()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        await f.MessageAsync("/notificar", 2);
        using (var scope = f.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ISystemAlertRepository>();
            await repo.CreateAsync(new SystemAlert { RestaurantId = 10, Title = "Estoque A", Message = "Reposição A", AlertType = SystemAlertType.StockBelowMinimum, GeneratedAtUtc = DateTime.UtcNow });
            await repo.CreateAsync(new SystemAlert { RestaurantId = 20, Title = "Segredo B", Message = "B", AlertType = SystemAlertType.StockBelowMinimum, GeneratedAtUtc = DateTime.UtcNow });
            await scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>().ProcessPendingAsync();
        }
        f.Transport.Sent.Should().ContainSingle(); f.Transport.Sent[0].Chat.Should().Be(123); f.Transport.Sent[0].Text.Should().NotContain("Segredo B");
        using (var scope = f.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISystemAlertRepository>().CreateAsync(new SystemAlert { RestaurantId = 10, Title = "Segundo A", Message = "A", AlertType = SystemAlertType.StockBelowMinimum, GeneratedAtUtc = DateTime.UtcNow });
        }
        await f.MessageAsync("/desvincular", 3);
        using (var scope = f.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>().ProcessPendingAsync();
        f.Transport.Sent.Should().ContainSingle();
    }
    [Fact] public async Task NotificationTest_RequiresOptIn_AndUsesTelegramHistory()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        using (var scope = f.Services.CreateScope())
        {
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
            var disabled = () => notifications.QueueTestAsync(100, "telegram");
            await disabled.Should().ThrowAsync<InvalidOperationException>();
        }
        await f.MessageAsync("/notificar", 2);
        using (var scope = f.Services.CreateScope())
        {
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
            await notifications.QueueTestAsync(100, "telegram");
            await notifications.ProcessPendingAsync();
            var history = await notifications.GetHistoryAsync(100);
            history.Should().ContainSingle().Which.Channel.Should().Be("telegram");
            history[0].IsTest.Should().BeTrue(); history[0].Status.Should().Be("Sent");
            var duplicate = () => notifications.QueueTestAsync(100, "telegram");
            await duplicate.Should().ThrowAsync<InvalidOperationException>();
        }
        f.Transport.Sent.Should().ContainSingle();
    }
    [Fact] public async Task ExpectedTelegramIdentity_PreventsDifferentUserFromConsumingLink()
    {
        using var f = new Fixture();
        string code;
        using (var scope = f.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<TelegramUserContext>().Set(100, 10, "Manager");
            var service = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var wrongBot = () => service.CreateLinkAsync(default, 999, 123);
            await wrongBot.Should().ThrowAsync<InvalidOperationException>();
            var link = await service.CreateLinkAsync(default, 12345, 123);
            code = link.Url.Split("start=")[1];
        }
        (await f.MessageAsync("/start " + code, 1, 456)).Should().Contain("outro ID");
        (await f.MessageAsync("/start " + code, 2, 123)).Should().Contain("Conta ISM conectada");
        (await f.MessageAsync("Cardápio", 3, 123)).Should().Contain("Prato A");
    }
    [Fact] public async Task UnlinkedTelegramUsersOnlyReceiveAdminEnrollmentInstructions()
    {
        using var f = new Fixture();
        var response = await f.MessageAsync("/id");
        response.Should().Contain("autorize seu ID").And.Contain("http://localhost:3000/integracoes")
            .And.NotContain("12345").And.NotContain("Dados do restaurante");
        (await f.MessageAsync("Agentes", 2)).Should().Contain("Somente o administrador").And.NotContain("orquestrador");
    }
    [Fact] public async Task Conversation_RoutesFinancialQuestionThroughOrchestrator()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        var response = await f.MessageAsync("Qual é o faturamento?", 2);
        response.Should().NotContain("Resposta de teste do assistente").And.Contain("Financeiro").And.Contain("•").And.Contain("0,00");
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        var message = await db.AssistantMessages.IgnoreQueryFilters().SingleAsync(m => m.Source == "Dados financeiros do ISM");
        message.Mode.Should().Be("deterministic"); message.RestaurantId.Should().Be(10);
    }
    [Fact] public async Task Conversation_FormatsStockQuantitiesWithoutAmbiguousDecimalSeparators()
    {
        using var f = new Fixture(); await f.ConnectAsync();
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            db.Products.Add(new ISM.Domain.Modules.Stock.Entities.Product
            {
                Id = 5, RestaurantId = 10, Name = "Salmão", Unit = "kg",
                CurrentQuantity = 6.125m, MinimumQuantity = 15.000m
            });
            await db.SaveChangesAsync();
        }
        var response = await f.MessageAsync("Qual é o saldo do produto 5?", 2);
        response.Should().Contain("Saldo atual: 6,125 kg").And.Contain("Mínimo: 15 kg").And.Contain("Fonte: Product");
    }
    [Fact] public void ResponseFormatting_EscapesHtmlAndKeepsSupportedEmphasis()
    {
        var formatted = ISM.Infrastructure.Telegram.TelegramTransport.FormatText("# Resumo\n**Estoque**\n<script>alert(1)</script>\n- Produto");
        formatted.Should().Contain("<b>Resumo</b>").And.Contain("<b>Estoque</b>").And.Contain("&lt;script&gt;").And.NotContain("<script>").And.Contain("• Produto");
    }
    [Fact] public async Task ApiIdentity_RejectsNonManagerOrForeignRestaurant()
    {
        using var f = new Fixture(); using var scope = f.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TelegramUserContext>().Set(100, 20, "Manager");
        var action = () => scope.ServiceProvider.GetRequiredService<ITelegramService>().CreateLinkAsync(default);
        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
