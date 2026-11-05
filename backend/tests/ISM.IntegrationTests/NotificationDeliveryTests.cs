using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ISM.Application.DTOs.Notifications;
using ISM.Application.Interfaces.Notifications;
using ISM.Domain.Entities;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using ISM.Infrastructure.Data.Context;
using ISM.Infrastructure.Notifications;
using ISM.Infrastructure.Repositories.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ISM.IntegrationTests;

public sealed class NotificationDeliveryTests
{
    [Fact]
    public async Task SmtpTransport_SendsRealMimeMessage_ToLocalSmtpServer()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var receive = ReceiveSmtpAsync(listener, timeout.Token);
        try
        {
            var transport = new NotificationTransport(Options.Create(new SmtpOptions
            {
                Enabled = true, Host = "127.0.0.1", Port = port, Security = "None", FromAddress = "sender@example.test"
            }), Options.Create(new WhatsAppOptions()), new HttpClient());
            await transport.SendEmailAsync(new NotificationDelivery
            {
                Id = 42, Recipient = "recipient@example.test", Subject = "ISM — Estoque",
                Body = "low-stock-test", HtmlBody = "<html><body><h1>Estoque baixo</h1></body></html>"
            }, timeout.Token);
            var mime = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(await receive)));
            mime.To.Mailboxes.Single().Address.Should().Be("recipient@example.test");
            mime.Subject.Should().Be("ISM — Estoque");
            mime.TextBody.Should().Contain("low-stock-test");
            mime.HtmlBody.Should().Contain("<h1>Estoque baixo</h1>");
        }
        finally { listener.Stop(); }
    }

    [Fact]
    public async Task WhatsAppTransport_AuthenticatesAndSendsToConfiguredSession()
    {
        var handler = new WhatsAppHandler();
        var transport = new NotificationTransport(Options.Create(new SmtpOptions()), Options.Create(new WhatsAppOptions
        {
            Enabled = true, BaseUrl = "http://whatsapp.test/", ApiKey = "test-key", Session = "default"
        }), new HttpClient(handler));
        await transport.SendWhatsAppAsync(new NotificationDelivery { Recipient = "+5511999999999", Body = "stock-alert" }, default);
        handler.Path.Should().Be("/api/sendText");
        handler.Key.Should().Be("test-key");
        using var payload = JsonDocument.Parse(handler.Body!);
        payload.RootElement.GetProperty("chatId").GetString().Should().Be("5511999999999@c.us");
        payload.RootElement.GetProperty("session").GetString().Should().Be("default");
        payload.RootElement.GetProperty("text").GetString().Should().Be("stock-alert");
    }

    [Fact]
    public async Task Alerts_QueueOnlyOptedInTenantManagers_AndDoNotResendAcceptedMessages()
    {
        await using var db = Database();
        db.Users.AddRange(
            User(1, 10, "Manager", true, true), User(2, 20, "Manager", true, true),
            User(3, 10, "Chef", true, true), User(4, 10, "Manager", false, false));
        await db.SaveChangesAsync();
        await new SystemAlertRepository(db).CreateAsync(Alert(10));
        var queued = await db.Set<NotificationDelivery>().ToListAsync();
        queued.Should().HaveCount(2).And.OnlyContain(d => d.UserId == 1);
        var transport = new RecordingTransport();
        var service = Service(db, transport);
        await service.ProcessPendingAsync();
        await service.ProcessPendingAsync();
        transport.Sent.Should().HaveCount(2);
        queued.Should().OnlyContain(d => d.Status == "Sent" && d.Attempts == 1);
    }

    [Fact]
    public async Task PendingDeliveries_AreCancelledAfterOptOut_AndRetryAfterTransportFailure()
    {
        await using var db = Database();
        var user = User(1, 10, "Manager", true, false);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        await new SystemAlertRepository(db).CreateAsync(Alert(10));
        var transport = new RecordingTransport { Fail = true };
        var service = Service(db, transport);
        await service.ProcessPendingAsync();
        var delivery = await db.Set<NotificationDelivery>().SingleAsync();
        delivery.Status.Should().Be("Pending");
        delivery.Attempts.Should().Be(1);
        delivery.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
        transport.Fail = false;
        await db.SaveChangesAsync();
        await service.ProcessPendingAsync();
        delivery.Status.Should().Be("Sent");
        delivery.Attempts.Should().Be(2);
        await new SystemAlertRepository(db).CreateAsync(Alert(10));
        user.EmailNotificationsEnabled = false;
        await db.SaveChangesAsync();
        await service.ProcessPendingAsync();
        (await db.Set<NotificationDelivery>().OrderByDescending(d => d.Id).FirstAsync()).Status.Should().Be("Cancelled");
        transport.Sent.Should().ContainSingle();
    }

    [Fact]
    public async Task UnconfiguredChannels_DoNotAttemptNetworkDelivery()
    {
        await using var db = Database();
        db.Users.Add(User(1, 10, "Manager", true, false));
        await db.SaveChangesAsync();
        await new SystemAlertRepository(db).CreateAsync(Alert(10));
        var transport = new RecordingTransport();
        var service = new NotificationDeliveryService(db, transport, Options.Create(new SmtpOptions()),
            Options.Create(new WhatsAppOptions()), NullLogger<NotificationDeliveryService>.Instance);
        await service.ProcessPendingAsync();
        transport.Sent.Should().BeEmpty();
        (await db.Set<NotificationDelivery>().SingleAsync()).Attempts.Should().Be(0);
    }

    [Fact]
    public async Task PresentationPreviews_AreTenantScoped_AndDoNotChangeOperationalData()
    {
        await using var db = Database();
        db.Users.AddRange(User(1, 10, "Manager", true, false), User(2, 20, "Manager", false, false));
        db.SystemAlerts.AddRange(Alert(10), Alert(20));
        await db.SaveChangesAsync();
        var ownAlert = await db.SystemAlerts.SingleAsync(a => a.RestaurantId == 10);
        var otherAlert = await db.SystemAlerts.SingleAsync(a => a.RestaurantId == 20);
        var service = Service(db, new RecordingTransport());
        var catalog = await service.GetScenariosAsync(1);
        catalog.Examples.Should().HaveCount(6);
        catalog.Alerts.Should().ContainSingle(a => a.Id == ownAlert.Id);
        var example = await service.PreviewAsync(1, "email", "stock-low", null);
        example.Html.Should().Contain("DADOS ILUSTRATIVOS");
        (await db.Set<NotificationDelivery>().CountAsync()).Should().Be(0);
        var action = () => service.PreviewAsync(1, "email", "existing-alert", otherAlert.Id);
        await action.Should().ThrowAsync<InvalidOperationException>();
        var real = await service.PreviewAsync(1, "whatsapp", "existing-alert", ownAlert.Id);
        real.Source.Should().Contain("ALERTA REAL");
        real.Text.Should().Contain(ownAlert.Message);
        var id = await service.QueueScenarioAsync(1, "email", "stock-low", null);
        var delivery = await db.Set<NotificationDelivery>().SingleAsync(d => d.Id == id);
        delivery.IsTest.Should().BeTrue();
        delivery.HtmlBody.Should().Contain("Ação recomendada");
        (await db.SystemAlerts.CountAsync()).Should().Be(2);
        var repeat = () => service.QueueScenarioAsync(1, "email", "stock-low", null);
        await repeat.Should().ThrowAsync<InvalidOperationException>();
        await service.QueueScenarioAsync(1, "email", "supplier-price", null);
    }

    private static IsmDbContext Database() => new(new DbContextOptionsBuilder<IsmDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static User User(int id, int restaurantId, string role, bool email, bool whatsapp) => new()
    {
        Id = id, RestaurantId = restaurantId, Role = role, Email = $"user{id}@example.test", IsActive = true,
        EmailNotificationsEnabled = email, WhatsAppNotificationsEnabled = whatsapp, WhatsAppPhone = "+5511999999999"
    };
    private static SystemAlert Alert(int restaurantId) => new()
    {
        RestaurantId = restaurantId, AlertType = SystemAlertType.StockBelowMinimum, Title = "Estoque baixo",
        Message = "Confira o estoque.", GeneratedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
    };
    private static NotificationDeliveryService Service(IsmDbContext db, RecordingTransport transport) => new(db, transport,
        Options.Create(new SmtpOptions { Enabled = true, Host = "smtp.test", FromAddress = "sender@example.test" }),
        Options.Create(new WhatsAppOptions { Enabled = true, ApiKey = "test-key" }), NullLogger<NotificationDeliveryService>.Instance);

    private sealed class RecordingTransport : INotificationTransport
    {
        public bool Fail { get; set; }
        public List<long> Sent { get; } = [];
        public Task SendEmailAsync(NotificationDelivery delivery, CancellationToken ct) => Send(delivery);
        public Task SendWhatsAppAsync(NotificationDelivery delivery, CancellationToken ct) => Send(delivery);
        private Task Send(NotificationDelivery delivery)
        {
            if (Fail) throw new IOException("test failure");
            Sent.Add(delivery.Id); return Task.CompletedTask;
        }
    }
    private sealed class WhatsAppHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? Path { get; private set; }
        public string? Key { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Path = request.RequestUri!.AbsolutePath;
            Key = request.Headers.GetValues("X-Api-Key").Single();
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"message-1\"}") };
        }
    }
    private static async Task<string> ReceiveSmtpAsync(TcpListener listener, CancellationToken ct)
    {
        using var client = await listener.AcceptTcpClientAsync(ct);
        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
        using var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteLineAsync("220 localhost SMTP test");
        var body = new StringBuilder();
        var data = false;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (data)
            {
                if (line == ".") { data = false; await writer.WriteLineAsync("250 accepted"); }
                else body.AppendLine(line.StartsWith("..") ? line[1..] : line);
            }
            else if (line.StartsWith("EHLO")) { await writer.WriteLineAsync("250-localhost"); await writer.WriteLineAsync("250 SIZE 1000000"); }
            else if (line == "DATA") { data = true; await writer.WriteLineAsync("354 send data"); }
            else if (line == "QUIT") { await writer.WriteLineAsync("221 bye"); break; }
            else await writer.WriteLineAsync("250 ok");
        }
        return body.ToString();
    }
}
