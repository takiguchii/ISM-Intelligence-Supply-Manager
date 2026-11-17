using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISM.Infrastructure.AI;

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
            return new(false, false, configuration.Provider, configuration.Model, "Conversação local desabilitada.");

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                configuration.Provider == "Ollama" ? $"{configuration.Endpoint}/api/tags" : configuration.Endpoint);
            ApplyAuthorization(request, configuration);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new(true, false, configuration.Provider, configuration.Model, $"HTTP {(int)response.StatusCode}");
            if (configuration.Provider == "Ollama" && !await OllamaModelIsAvailableAsync(response, configuration.Model, cancellationToken))
                return new(true, false, configuration.Provider, configuration.Model, $"Modelo '{configuration.Model}' não está instalado no Ollama.");
            return new(true, true, configuration.Provider, configuration.Model, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(true, false, configuration.Provider, configuration.Model, "Timeout");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Provider de IA indisponível para o ISM Assist");
            return new(true, false, configuration.Provider, configuration.Model, "Provider indisponível");
        }
    }

    public async Task<LlmTextResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        if (!configuration.Enabled) return new(false, null, "Provider desabilitado");

        try
        {
            using var request = BuildRequest(configuration, systemPrompt, userPrompt);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return new(false, null, $"HTTP {(int)response.StatusCode}");

            var content = await ExtractTextAsync(response, configuration.Provider, cancellationToken);
            return string.IsNullOrWhiteSpace(content)
                ? new(false, null, "Resposta vazia")
                : new(true, content.Trim(), null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, null, "Timeout");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao consultar provider de IA");
            return new(false, null, "Provider indisponível");
        }
    }

    private HttpRequestMessage BuildRequest(RuntimeConfiguration configuration, string systemPrompt, string userPrompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, configuration.Provider switch
        {
            "Ollama" => $"{configuration.Endpoint}/api/chat",
            "Anthropic Claude" => $"{configuration.Endpoint}/messages",
            _ => $"{configuration.Endpoint}/chat/completions"
        });
        ApplyAuthorization(request, configuration);

        object payload = configuration.Provider switch
        {
            "Ollama" => new
            {
                model = configuration.Model,
                stream = false,
                messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } },
                options = new { num_predict = _options.MaxOutputTokens }
            },
            "Anthropic Claude" => new
            {
                model = configuration.Model,
                max_tokens = _options.MaxOutputTokens,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userPrompt } }
            },
            _ => new
            {
                model = configuration.Model,
                max_tokens = _options.MaxOutputTokens,
                messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    private static async Task<string?> ExtractTextAsync(HttpResponseMessage response, string provider, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (provider == "Ollama" && document.RootElement.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var ollamaText))
            return ollamaText.GetString();
        if (provider == "Anthropic Claude" && document.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0 && content[0].TryGetProperty("text", out var anthropicText))
            return anthropicText.GetString();
        if (document.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 && choices[0].TryGetProperty("message", out var choiceMessage) && choiceMessage.TryGetProperty("content", out var openAiText))
            return openAiText.GetString();
        return null;
    }

    private static async Task<bool> OllamaModelIsAvailableAsync(HttpResponseMessage response, string model, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            return false;
        return models.EnumerateArray().Any(item => item.TryGetProperty("name", out var name) && string.Equals(name.GetString(), model, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyAuthorization(HttpRequestMessage request, RuntimeConfiguration configuration)
    {
        if (configuration.Provider == "Ollama" || string.IsNullOrWhiteSpace(configuration.ApiKey)) return;
        if (configuration.Provider == "Anthropic Claude")
        {
            request.Headers.Add("x-api-key", configuration.ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            return;
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.ApiKey);
    }

    private async Task<RuntimeConfiguration> ResolveConfigurationAsync(CancellationToken cancellationToken)
    {
        var endpoint = _options.OllamaBaseUrl;
        var model = _options.Model;
        string? apiKey = null;

        if (currentUser.RestaurantId is > 0 && await restaurants.GetRestaurantByIdAsync(currentUser.RestaurantId.Value, cancellationToken) is { } restaurant)
        {
            endpoint = string.IsNullOrWhiteSpace(restaurant.AgentApiEndpoint) ? endpoint : restaurant.AgentApiEndpoint;
            model = string.IsNullOrWhiteSpace(restaurant.AgentModel) ? model : restaurant.AgentModel;
            apiKey = restaurant.AgentApiKey;
        }

        endpoint = endpoint.TrimEnd('/');
        var provider = endpoint.Contains("11434", StringComparison.OrdinalIgnoreCase)
            ? "Ollama"
            : endpoint.Contains("anthropic.com", StringComparison.OrdinalIgnoreCase) ? "Anthropic Claude" : "API compatível";
        return new(_options.Enabled, endpoint, model, apiKey, provider);
    }

    private sealed record RuntimeConfiguration(bool Enabled, string Endpoint, string Model, string? ApiKey, string Provider);
}
