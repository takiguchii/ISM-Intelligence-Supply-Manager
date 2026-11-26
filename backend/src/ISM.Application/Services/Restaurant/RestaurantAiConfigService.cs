using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISM.Application.DTOs.Restaurant;
using ISM.Application.Interfaces.Restaurant;
using ISM.Application.Options;
using ISM.Domain.Interfaces;
using RestaurantEntity = ISM.Domain.Entities.Restaurant;

namespace ISM.Application.Services.Restaurant;

public sealed class RestaurantAiConfigService : IRestaurantAiConfigService
{
    private const int RequestTimeoutSeconds = 240;

    private readonly IRestaurantRepository _restaurantRepository;

    public RestaurantAiConfigService(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<RestaurantAiPhotoConfigDto> GetByRestaurantIdAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);

        var hasCustomKey = !string.IsNullOrWhiteSpace(restaurant.PhotoImportApiKey);
        string? keyLast4Digits = null;
        if (hasCustomKey && restaurant.PhotoImportApiKey!.Length >= 4)
        {
            keyLast4Digits = restaurant.PhotoImportApiKey[^4..];
        }

        var warnings = new List<string>();
        if (hasCustomKey && string.IsNullOrWhiteSpace(restaurant.PhotoImportApiEndpoint))
        {
            warnings.Add("Chave API configurada sem endpoint específico (será usado fallback).");
        }

        return new RestaurantAiPhotoConfigDto
        {
            HasCustomKey = hasCustomKey,
            KeyLast4Digits = keyLast4Digits,
            Provider = InferProvider(restaurant.PhotoImportProvider, restaurant.PhotoImportApiEndpoint, AiProviderCatalog.Gemini, restaurant.PhotoImportModel),
            ApiEndpoint = AiProviderCatalog.Endpoint(InferProvider(restaurant.PhotoImportProvider, restaurant.PhotoImportApiEndpoint, AiProviderCatalog.Gemini, restaurant.PhotoImportModel)),
            Model = restaurant.PhotoImportModel,
            IsUsingRestaurantConfig = hasCustomKey,
            IsUsingGlobalFallback = !hasCustomKey,
            Warnings = warnings.Count > 0 ? warnings : null
        };
    }

    public async Task UpdateByRestaurantIdAsync(int restaurantId, UpdateRestaurantAiPhotoConfigDto dto, CancellationToken ct)
    {
        var restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);

        if (dto.ClearKey)
        {
            restaurant.PhotoImportApiKey = null;
        }
        else if (dto.ApiKey is not null)
        {
            restaurant.PhotoImportApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? null : dto.ApiKey.Trim();
        }

        var provider = AiProviderCatalog.Normalize(dto.Provider ?? restaurant.PhotoImportProvider, InferProvider(restaurant.PhotoImportProvider, restaurant.PhotoImportApiEndpoint, AiProviderCatalog.Gemini, restaurant.PhotoImportModel));
        restaurant.PhotoImportProvider = provider;
        restaurant.PhotoImportApiEndpoint = AiProviderCatalog.Endpoint(provider);

        if (dto.Model is not null)
        {
            var model = dto.Model.Trim();
            restaurant.PhotoImportModel = string.IsNullOrWhiteSpace(model) ? AiProviderCatalog.DefaultModel(provider) : model;
        }
        else if (string.IsNullOrWhiteSpace(restaurant.PhotoImportModel)) restaurant.PhotoImportModel = AiProviderCatalog.DefaultModel(provider);

        restaurant.UpdatedAtUtc = DateTime.UtcNow;
        _restaurantRepository.Update(restaurant);
        await _restaurantRepository.SaveChangesAsync(ct);
    }

    public async Task<TestRestaurantAiPhotoConfigResultDto> TestByRestaurantIdAsync(
        int restaurantId,
        TestRestaurantAiPhotoConfigRequestDto? req,
        CancellationToken ct)
    {
        var restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);
        var provider = AiProviderCatalog.Normalize(req?.OverrideProvider ?? restaurant.PhotoImportProvider, InferProvider(restaurant.PhotoImportProvider, restaurant.PhotoImportApiEndpoint, AiProviderCatalog.Gemini, req?.OverrideModel ?? restaurant.PhotoImportModel));
        var model = string.IsNullOrWhiteSpace(req?.OverrideModel) ? restaurant.PhotoImportModel ?? AiProviderCatalog.DefaultModel(provider) : req!.OverrideModel!.Trim();
        var apiKey = string.IsNullOrWhiteSpace(req?.OverrideApiKey) ? restaurant.PhotoImportApiKey?.Trim() : req!.OverrideApiKey!.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return new TestRestaurantAiPhotoConfigResultDto { Success = false, Provider = provider, NormalizedModel = model, NormalizedEndpoint = AiProviderCatalog.Endpoint(provider), ErrorMessage = "Informe a chave de API deste provedor.", LatencyMs = 0 };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var pixel = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j3ioAAAAASUVORK5CYII=");
            using var request = AiProviderCatalog.CreateRequest(provider, model, apiKey, "Responda de forma curta.", "Descreva a imagem em uma palavra.", 32, pixel, "image/png", sessionId: $"ses_ism_photo_test_{restaurantId}");
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var success = response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(AiProviderCatalog.ExtractText(provider, body));
            var error = success ? null : GetProviderErrorMessage(body) ?? $"Falha na validação: HTTP {(int)response.StatusCode}.";
            return new TestRestaurantAiPhotoConfigResultDto { Success = success, Provider = provider, NormalizedModel = model, NormalizedEndpoint = AiProviderCatalog.Endpoint(provider), ErrorMessage = error, HttpStatusFromProvider = (int)response.StatusCode, LatencyMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return new TestRestaurantAiPhotoConfigResultDto { Success = false, Provider = provider, NormalizedModel = model, NormalizedEndpoint = AiProviderCatalog.Endpoint(provider), ErrorMessage = ex is TaskCanceledException ? "Tempo esgotado ao testar o provedor." : ex.Message, LatencyMs = stopwatch.ElapsedMilliseconds };
        }
    }

    public async Task<RestaurantAgentConfigDto> GetAgentByRestaurantIdAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);
        var provider = InferProvider(restaurant.AgentProvider, restaurant.AgentApiEndpoint, AiProviderCatalog.OpenCodeGo, restaurant.AgentModel);
        var hasKey = !string.IsNullOrWhiteSpace(restaurant.AgentApiKey);
        return new RestaurantAgentConfigDto
        {
            HasApiKey = hasKey,
            KeyLast4Digits = hasKey && restaurant.AgentApiKey!.Length >= 4 ? restaurant.AgentApiKey[^4..] : null,
            Provider = provider,
            ApiEndpoint = AiProviderCatalog.Endpoint(provider),
            Model = restaurant.AgentModel ?? AiProviderCatalog.DefaultModel(provider)
        };
    }

    public async Task UpdateAgentByRestaurantIdAsync(int restaurantId, UpdateRestaurantAgentConfigDto dto, CancellationToken ct)
    {
        var restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);
        if (dto.ClearKey) restaurant.AgentApiKey = null;
        else if (dto.ApiKey is not null) restaurant.AgentApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? null : dto.ApiKey.Trim();
        var provider = AiProviderCatalog.Normalize(dto.Provider ?? restaurant.AgentProvider, InferProvider(restaurant.AgentProvider, restaurant.AgentApiEndpoint, AiProviderCatalog.OpenCodeGo, restaurant.AgentModel));
        restaurant.AgentProvider = provider;
        restaurant.AgentApiEndpoint = AiProviderCatalog.Endpoint(provider);
        if (dto.Model is not null) restaurant.AgentModel = string.IsNullOrWhiteSpace(dto.Model) ? AiProviderCatalog.DefaultModel(provider) : dto.Model.Trim();
        else if (string.IsNullOrWhiteSpace(restaurant.AgentModel)) restaurant.AgentModel = AiProviderCatalog.DefaultModel(provider);
        restaurant.UpdatedAtUtc = DateTime.UtcNow;
        _restaurantRepository.Update(restaurant);
        await _restaurantRepository.SaveChangesAsync(ct);
    }

    public async Task<TestRestaurantAgentConfigResultDto> TestAgentByRestaurantIdAsync(int restaurantId, TestRestaurantAgentConfigRequestDto? req, CancellationToken ct)
    {
        var restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);
        var provider = AiProviderCatalog.Normalize(req?.OverrideProvider ?? restaurant.AgentProvider, InferProvider(restaurant.AgentProvider, restaurant.AgentApiEndpoint, AiProviderCatalog.OpenCodeGo, req?.OverrideModel ?? restaurant.AgentModel));
        var model = string.IsNullOrWhiteSpace(req?.OverrideModel) ? restaurant.AgentModel ?? AiProviderCatalog.DefaultModel(provider) : req!.OverrideModel!.Trim();
        var apiKey = string.IsNullOrWhiteSpace(req?.OverrideApiKey) ? restaurant.AgentApiKey?.Trim() : req!.OverrideApiKey!.Trim();
        if (string.IsNullOrWhiteSpace(apiKey)) return AgentFailure(provider, AiProviderCatalog.Endpoint(provider), model, "Informe a chave de API deste provedor.", Stopwatch.StartNew());

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = AiProviderCatalog.CreateRequest(provider, model, apiKey, "Responda somente pong.", "ping", 16, sessionId: $"ses_ism_agent_test_{restaurantId}");
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var success = response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(AiProviderCatalog.ExtractText(provider, body));
            var error = success ? null : GetProviderErrorMessage(body) ?? $"Falha na validação: HTTP {(int)response.StatusCode}.";
            return new TestRestaurantAgentConfigResultDto { Success = success, Provider = provider, NormalizedEndpoint = AiProviderCatalog.Endpoint(provider), NormalizedModel = model, ErrorMessage = error, HttpStatusFromProvider = (int)response.StatusCode, LatencyMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return AgentFailure(provider, AiProviderCatalog.Endpoint(provider), model, ex is TaskCanceledException ? "Tempo esgotado ao testar o provedor." : ex.Message, stopwatch);
        }
    }

    private static string InferProvider(string? provider, string? endpoint, string fallback, string? model = null)
    {
        if (!string.IsNullOrWhiteSpace(provider)) return AiProviderCatalog.Normalize(provider, fallback);
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
        if (endpoint?.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Gemini;
        if (endpoint?.Contains("anthropic.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Anthropic;
        if (endpoint?.Contains("opencode.ai", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenCodeGo;
        if (endpoint?.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenAI;
        if (endpoint?.Contains("api.deepseek.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.DeepSeek;
        if (endpoint?.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.OpenRouter;
        if (endpoint?.Contains("api.groq.com", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Groq;
        if (endpoint?.Contains("api.mistral.ai", StringComparison.OrdinalIgnoreCase) == true) return AiProviderCatalog.Mistral;
        return fallback;
    }

    private static string? GetProviderErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String) return LimitProviderError(error.GetString());
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
                    return LimitProviderError(message.GetString());
            }
            if (root.TryGetProperty("message", out var rootMessage)) return LimitProviderError(rootMessage.GetString());
        }
        catch (JsonException) { }
        return LimitProviderError(body);
    }

    private static string? LimitProviderError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        var sanitized = Regex.Replace(message, "(?i)(api[_-]?key|authorization|bearer)\\s*[:=]\\s*[^\\s,;]+", "$1=[oculto]");
        return sanitized.Length > 400 ? sanitized[..400] : sanitized;
    }

    private static TestRestaurantAgentConfigResultDto AgentFailure(string provider, string endpoint, string model, string error, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return new TestRestaurantAgentConfigResultDto { Success = false, Provider = provider, NormalizedEndpoint = endpoint, NormalizedModel = model, ErrorMessage = error, LatencyMs = stopwatch.ElapsedMilliseconds };
    }

    private async Task<RestaurantEntity> GetRestaurantOrThrowAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, ct);
        if (restaurant == null)
        {
            throw new InvalidOperationException($"Restaurante {restaurantId} não encontrado.");
        }
        return restaurant;
    }
}
