using System.Net.Http.Json;
using System.Text.Json;
using ISM.Application.DTOs.Notifications;
using ISM.Application.Interfaces.Notifications;
using ISM.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ISM.Infrastructure.Notifications;

public sealed class NotificationTransport(IOptions<SmtpOptions> smtpOptions,
    IOptions<WhatsAppOptions> whatsappOptions, HttpClient http) : INotificationTransport, IWhatsAppConnection
{
    private readonly SmtpOptions _smtp = smtpOptions.Value;
    private readonly WhatsAppOptions _whatsapp = whatsappOptions.Value;

    public async Task SendEmailAsync(NotificationDelivery delivery, CancellationToken ct)
    {
        if (!_smtp.IsConfigured) throw new InvalidOperationException("SMTP não configurado.");
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(delivery.Recipient));
        message.Subject = delivery.Subject;
        message.MessageId = $"ism-{delivery.Id}@notifications.local";
        var body = new BodyBuilder { TextBody = delivery.Body, HtmlBody = delivery.HtmlBody };
        message.Body = body.ToMessageBody();
        using var client = new SmtpClient { Timeout = 20000 };
        var security = Enum.Parse<SecureSocketOptions>(_smtp.Security);
        await client.ConnectAsync(_smtp.Host, _smtp.Port, security, ct);
        if (!string.IsNullOrWhiteSpace(_smtp.Username))
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
        await client.SendAsync(message, ct);
        // SendAsync já confirmou a aceitação pelo SMTP; uma falha no QUIT não deve reenviar.
        try { await client.DisconnectAsync(true, ct); }
        catch (Exception) when (!ct.IsCancellationRequested) { }
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        if (!_whatsapp.IsConfigured) throw new InvalidOperationException("WhatsApp não configurado.");
        var request = new HttpRequestMessage(method, new Uri(new Uri(_whatsapp.BaseUrl.TrimEnd('/') + "/"), path));
        request.Headers.Add("X-Api-Key", _whatsapp.ApiKey);
        return request;
    }

    public async Task SendWhatsAppAsync(NotificationDelivery delivery, CancellationToken ct)
    {
        using var request = Request(HttpMethod.Post, "api/sendText");
        request.Content = JsonContent.Create(new
        {
            session = _whatsapp.Session, chatId = delivery.Recipient.TrimStart('+') + "@c.us",
            text = delivery.Body, linkPreview = false
        });
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var result = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!result.RootElement.TryGetProperty("id", out var id) || id.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException("O WhatsApp não confirmou o envio.");
    }

    public async Task<string> GetStatusAsync(CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, $"api/sessions/{Uri.EscapeDataString(_whatsapp.Session)}");
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return "STOPPED";
        response.EnsureSuccessStatusCode();
        using var result = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return result.RootElement.GetProperty("status").GetString() ?? "UNKNOWN";
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var state = await GetStatusAsync(ct);
        if (state is "WORKING" or "SCAN_QR_CODE" or "STARTING") return;
        using var create = Request(HttpMethod.Post, "api/sessions");
        create.Content = JsonContent.Create(new { name = _whatsapp.Session, start = true });
        using var created = await http.SendAsync(create, ct);
        if (created.IsSuccessStatusCode) return;
        if (created.StatusCode is not (System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.UnprocessableEntity))
            created.EnsureSuccessStatusCode();
        using var start = Request(HttpMethod.Post, $"api/sessions/{Uri.EscapeDataString(_whatsapp.Session)}/start");
        using var started = await http.SendAsync(start, ct);
        started.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> GetQrAsync(CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, $"api/{Uri.EscapeDataString(_whatsapp.Session)}/auth/qr");
        request.Headers.Accept.ParseAdd("image/png");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentType?.MediaType != "image/png")
            throw new InvalidOperationException("QR Code indisponível.");
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length > 1000000) throw new InvalidOperationException("QR Code inválido.");
        return bytes;
    }
}
