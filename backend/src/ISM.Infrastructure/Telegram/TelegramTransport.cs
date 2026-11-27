using System.Net.Http.Json;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using ISM.Application.Interfaces.Telegram;
using Microsoft.Extensions.Options;

namespace ISM.Infrastructure.Telegram;

// HttpClient próprio: o token faz parte da URL exigida pelo Telegram e não pode
// passar pelo logging padrão do IHttpClientFactory.
public sealed class TelegramTransport(IOptions<TelegramOptions> options) : ITelegramTransport, IDisposable
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(35) };
    private async Task<JsonDocument> CallAsync(string method, object body, CancellationToken ct)
    {
        if (!options.Value.IsConfigured) throw new InvalidOperationException("Telegram não configurado.");
        using var response = await http.PostAsJsonAsync($"https://api.telegram.org/bot{options.Value.BotToken}/{method}", body, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Telegram recusou a operação ({(int)response.StatusCode}).");
        var result = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!result.RootElement.GetProperty("ok").GetBoolean()) { result.Dispose(); throw new InvalidOperationException("Telegram não confirmou a operação."); }
        return result;
    }
    public async Task ConfigureAsync(CancellationToken ct)
    {
        using var me = await CallAsync("getMe", new { }, ct);
        var bot = me.RootElement.GetProperty("result");
        if (!string.Equals(bot.GetProperty("username").GetString(), options.Value.BotUsername, StringComparison.OrdinalIgnoreCase)
            || (options.Value.ResolvedBotId.HasValue && bot.GetProperty("id").GetInt64() != options.Value.ResolvedBotId.Value))
            throw new InvalidOperationException("Token e nome do bot não correspondem.");
        using var webhook = await CallAsync("getWebhookInfo", new { }, ct);
        if (!string.IsNullOrEmpty(webhook.RootElement.GetProperty("result").GetProperty("url").GetString()))
            throw new InvalidOperationException("Este bot já possui webhook. O polling não foi iniciado.");
        var commands = new[] { ("ajuda", "Comandos disponíveis"), ("estoque", "Consultar estoque"), ("financeiro", "Resumo financeiro"), ("pedidos", "Últimos pedidos"), ("pedido", "Detalhes de um pedido"), ("cardapio", "Pratos disponíveis"), ("alertas", "Alertas ativos"), ("notificar", "Ativar alertas"), ("parar", "Pausar alertas"), ("nova", "Reiniciar conversa"), ("desvincular", "Remover acesso") };
        using var configured = await CallAsync("setMyCommands", new { commands = commands.Select(c => new { command = c.Item1, description = c.Item2 }) }, ct);
        using var description = await CallAsync("setMyDescription", new { description = "Assistente da ISM para restaurantes. Converse, consulte pedidos e cardápio e acompanhe alertas. Conecte sua conta em Integrações → Telegram no ISM." }, ct);
    }
    public async Task<IReadOnlyList<TelegramIncoming>> GetUpdatesAsync(long offset, CancellationToken ct)
    {
        using var result = await CallAsync("getUpdates", new { offset, timeout = 20, limit = 1, allowed_updates = new[] { "message" } }, ct);
        return result.RootElement.GetProperty("result").EnumerateArray().Select(update =>
        {
            var id = update.GetProperty("update_id").GetInt64();
            if (!update.TryGetProperty("message", out var message)) return new TelegramIncoming(id, 0, 0, "unsupported", "");
            var chat = message.GetProperty("chat");
            var sender = message.TryGetProperty("from", out var from) ? from.GetProperty("id").GetInt64() : 0;
            return new TelegramIncoming(id, chat.GetProperty("id").GetInt64(), sender, chat.GetProperty("type").GetString() ?? "", message.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "/ajuda");
        }).ToList();
    }
    public async Task SendAsync(long chatId, string text, CancellationToken ct)
    {
        if (text.Length > 11000) text = text[..11000] + "\nConfira mais detalhes no ISM.";
        while (text.Length > 0)
        {
            var count = Math.Min(3800, text.Length);
            if (char.IsHighSurrogate(text[count - 1])) count--;
            using var sent = await CallAsync("sendMessage", new { chat_id = chatId, text = FormatText(text[..count]), parse_mode = "HTML", protect_content = true,
                reply_markup = new { keyboard = new[] { new[] { new { text = "/estoque" }, new { text = "/financeiro" } }, new[] { new { text = "/pedidos" }, new { text = "/cardapio" } }, new[] { new { text = "/alertas" }, new { text = "/ajuda" } } }, resize_keyboard = true, input_field_placeholder = "Pergunte à ISM ou escolha um atalho" }, link_preview_options = new { is_disabled = true } }, ct);
            text = text[count..];
        }
    }
    public static string FormatText(string text)
    {
        var escaped = WebUtility.HtmlEncode(text);
        escaped = Regex.Replace(escaped, @"\*\*([^*\n]+)\*\*", "<b>$1</b>");
        escaped = Regex.Replace(escaped, @"(?m)^#{1,3} (.+)$", "<b>$1</b>");
        escaped = Regex.Replace(escaped, @"(?m)^- ", "• ");
        var newline = escaped.IndexOf('\n');
        if (newline > 0 && newline < 100 && !escaped.StartsWith("<b>"))
            escaped = "<b>" + escaped[..newline] + "</b>" + escaped[newline..];
        escaped = Regex.Replace(escaped, @"(?m)^(Status|Total dos itens|Prioridade|Ação recomendada):", "<b>$1:</b>");
        return escaped;
    }
    public void Dispose() => http.Dispose();
}
