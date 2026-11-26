using System.Text;
using System.Text.Json;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISM.Infrastructure.AI;

/// <summary>Cliente de texto/visão do modelo configurado para o restaurante.</summary>
public sealed class OllamaTextProvider(
    HttpClient httpClient,
    IOptions<AssistantAiOptions> options,
    ICurrentUser currentUser,
    IRestaurantRepository restaurants,
    ILogger<OllamaTextProvider> logger) : ILlmTextProvider
{
    private readonly AssistantAiOptions _options = options.Value;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => (await GetStatusAsync(cancellationToken)).Reachable;

    public async Task<LlmProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        if (!configuration.Enabled)
            return new(false, false, configuration.Provider, configuration.Model, "Conversação por IA desativada.");

        try
        {
            if (configuration.Provider == "Ollama")
            {
                using var ollamaRequest = new HttpRequestMessage(HttpMethod.Get, $"{configuration.Endpoint}/api/tags");
                using var ollamaResponse = await httpClient.SendAsync(ollamaRequest, cancellationToken);
                if (!ollamaResponse.IsSuccessStatusCode)
                    return new(true, false, configuration.Provider, configuration.Model, $"HTTP {(int)ollamaResponse.StatusCode}");
                var installed = await OllamaModelIsAvailableAsync(ollamaResponse, configuration.Model, cancellationToken);
                return new(true, installed, configuration.Provider, configuration.Model, installed ? null : $"Modelo '{configuration.Model}' não instalado no Ollama.");
            }

            using var externalRequest = CreateExternalRequest(configuration, "Responda somente pong.", "ping");
            using var externalResponse = await httpClient.SendAsync(externalRequest, cancellationToken);
            if (!externalResponse.IsSuccessStatusCode)
                return new(true, false, configuration.Provider, configuration.Model, $"HTTP {(int)externalResponse.StatusCode}");
            var text = await AiProviderCatalog.ExtractTextAsync(externalResponse, configuration.Provider, cancellationToken);
            return new(true, !string.IsNullOrWhiteSpace(text), configuration.Provider, configuration.Model, string.IsNullOrWhiteSpace(text) ? "Resposta vazia." : null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(true, false, configuration.Provider, configuration.Model, "Tempo esgotado.");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Provider de IA indisponível para o ISM Assist");
            return new(true, false, configuration.Provider, configuration.Model, "Provider indisponível.");
        }
    }

    public Task<LlmTextResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        => CompleteCoreAsync(systemPrompt, userPrompt, null, null, cancellationToken);

    public Task<LlmTextResult> CompleteWithImageAsync(string systemPrompt, string userPrompt, byte[] imageBytes, string contentType, CancellationToken cancellationToken = default)
        => CompleteCoreAsync(systemPrompt, userPrompt, imageBytes, contentType, cancellationToken);

    private async Task<LlmTextResult> CompleteCoreAsync(string systemPrompt, string userPrompt, byte[]? imageBytes, string? contentType, CancellationToken cancellationToken)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        if (!configuration.Enabled) return new(false, null, "Provider desabilitado");

        try
        {
            using var request = configuration.Provider == "Ollama"
                ? CreateOllamaRequest(configuration, systemPrompt, userPrompt, imageBytes)
                : CreateExternalRequest(configuration, systemPrompt, userPrompt, imageBytes, contentType);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Falha no provedor {Provider}: HTTP {Status}. Resposta: {Body}", configuration.Provider, (int)response.StatusCode, body.Length > 400 ? body[..400] : body);
                return new(false, null, $"HTTP {(int)response.StatusCode}");
            }

            var content = configuration.Provider == "Ollama"
                ? ExtractOllamaText(body)
                : AiProviderCatalog.ExtractText(configuration.Provider, body);
            return string.IsNullOrWhiteSpace(content) ? new(false, null, "Resposta vazia") : new(true, content.Trim(), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, null, "Tempo esgotado");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao consultar provider de IA");
            return new(false, null, "Provider indisponível");
        }
    }

    private HttpRequestMessage CreateExternalRequest(RuntimeConfiguration configuration, string systemPrompt, string userPrompt, byte[]? imageBytes = null, string? contentType = null)
        => AiProviderCatalog.CreateRequest(configuration.Provider, configuration.Model, configuration.ApiKey, systemPrompt, userPrompt, _options.MaxOutputTokens, imageBytes, contentType, sessionId: $"ses_ism_{currentUser.RestaurantId ?? 0}_{currentUser.UserId}");

    private static HttpRequestMessage CreateOllamaRequest(RuntimeConfiguration configuration, string systemPrompt, string userPrompt, byte[]? imageBytes)
    {
        var messages = imageBytes is null
            ? new object[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } }
            : new object[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt, images = new[] { Convert.ToBase64String(imageBytes) } } };
        var request = new HttpRequestMessage(HttpMethod.Post, $"{configuration.Endpoint}/api/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { model = configuration.Model, stream = false, think = false, messages, options = new { num_predict = 512 } }), Encoding.UTF8, "application/json")
        };
        return request;
    }

    private async Task<RuntimeConfiguration> ResolveConfigurationAsync(CancellationToken cancellationToken)
    {
        var endpoint = _options.OllamaBaseUrl.TrimEnd('/');
        var model = _options.Model;
        var provider = "Ollama";
        string? apiKey = null;
        var enabled = _options.Enabled;

        if (currentUser.RestaurantId is > 0 && await restaurants.GetRestaurantByIdAsync(currentUser.RestaurantId.Value, cancellationToken) is { } restaurant)
        {
            provider = InferProvider(restaurant.AgentProvider, restaurant.AgentApiEndpoint, restaurant.AgentModel);
            endpoint = provider == "Ollama" ? (string.IsNullOrWhiteSpace(restaurant.AgentApiEndpoint) ? endpoint : restaurant.AgentApiEndpoint.TrimEnd('/')) : AiProviderCatalog.Endpoint(provider);
            model = string.IsNullOrWhiteSpace(restaurant.AgentModel) ? AiProviderCatalog.DefaultModel(provider) : restaurant.AgentModel;
            apiKey = restaurant.AgentApiKey;
            enabled = enabled || !string.IsNullOrWhiteSpace(apiKey);
        }

        return new(enabled, endpoint, model, apiKey, provider);
    }

    private static string InferProvider(string? provider, string? endpoint, string? model)
    {
        if (!string.IsNullOrWhiteSpace(provider)) return AiProviderCatalog.Normalize(provider);
        var modelName = model?.Trim();
        if (modelName?.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Gemini;
        if (modelName?.StartsWith("claude-", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Anthropic;
        if (modelName?.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenAI;
        if (modelName?.StartsWith("deepseek-v4.", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenCodeGo;
        if (modelName?.StartsWith("deepseek-", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.DeepSeek;
        if (modelName?.StartsWith("llama-", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Groq;
        if (modelName?.StartsWith("mistral-", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Mistral;
        if (modelName?.StartsWith("openai/", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenRouter;
        if (modelName?.Contains(':') == true) return "Ollama";
        if (endpoint?.Contains("opencode.ai", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenCodeGo;
        if (endpoint?.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Gemini;
        if (endpoint?.Contains("anthropic.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Anthropic;
        if (endpoint?.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenAI;
        if (endpoint?.Contains("api.deepseek.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.DeepSeek;
        if (endpoint?.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenRouter;
        if (endpoint?.Contains("api.groq.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Groq;
        if (endpoint?.Contains("api.mistral.ai", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Mistral;
        return "Ollama";
    }

    private static async Task<bool> OllamaModelIsAvailableAsync(HttpResponseMessage response, string model, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array &&
               models.EnumerateArray().Any(item => item.TryGetProperty("name", out var name) && string.Equals(name.GetString(), model, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractOllamaText(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content) ? content.GetString() : null;
    }

    private sealed record RuntimeConfiguration(bool Enabled, string Endpoint, string Model, string? ApiKey, string Provider);
}
