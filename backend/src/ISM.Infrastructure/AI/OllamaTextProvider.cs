using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISM.Infrastructure.AI;

public sealed class OllamaTextProvider(HttpClient httpClient, IOptions<AssistantAiOptions> options, ILogger<OllamaTextProvider> logger) : ILlmTextProvider
{
    private readonly AssistantAiOptions _options = options.Value;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return false;
        try { return (await httpClient.GetAsync("api/tags", cancellationToken)).IsSuccessStatusCode; }
        catch (Exception ex) { logger.LogDebug(ex, "Ollama indisponível para o ISM Assist"); return false; }
    }

    public async Task<LlmTextResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return new(false, null, "Provider desabilitado");
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/chat", new
            {
                model = _options.Model,
                stream = false,
                messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } },
                options = new { num_predict = _options.MaxOutputTokens }
            }, cancellationToken);
            if (!response.IsSuccessStatusCode) return new(false, null, $"HTTP {(int)response.StatusCode}");
            var payload = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);
            return string.IsNullOrWhiteSpace(payload?.Message?.Content)
                ? new(false, null, "Resposta vazia")
                : new(true, payload.Message.Content.Trim(), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(false, null, "Timeout"); }
        catch (Exception ex) { logger.LogWarning(ex, "Falha ao consultar Ollama"); return new(false, null, "Provider indisponível"); }
    }

    private sealed record OllamaResponse([property: JsonPropertyName("message")] OllamaMessage? Message);
    private sealed record OllamaMessage([property: JsonPropertyName("content")] string? Content);
}
