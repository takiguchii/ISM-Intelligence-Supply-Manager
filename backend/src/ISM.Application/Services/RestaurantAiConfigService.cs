using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services;

public sealed class RestaurantAiConfigService : IRestaurantAiConfigService
{
    private const string DefaultModel = "gemini-3.5-flash-lite";
    private const string RequiredEndpointHost = "generativelanguage.googleapis.com";
    private const int RequestTimeoutSeconds = 15;

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
            ApiEndpoint = restaurant.PhotoImportApiEndpoint,
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

        if (dto.ApiEndpoint is not null)
        {
            var endpoint = dto.ApiEndpoint.Trim();
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                if (!endpoint.Contains(RequiredEndpointHost, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Endpoint inválido. Deve conter '{RequiredEndpointHost}'.");
                }

                if (!endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = "https://" + endpoint.TrimStart('/');
                }

                restaurant.PhotoImportApiEndpoint = endpoint.TrimEnd('/');
            }
            else
            {
                restaurant.PhotoImportApiEndpoint = null;
            }
        }

        if (dto.Model is not null)
        {
            var model = dto.Model.Trim();
            restaurant.PhotoImportModel = string.IsNullOrWhiteSpace(model) ? null : model;
        }

        restaurant.UpdatedAtUtc = DateTime.UtcNow;
        _restaurantRepository.Update(restaurant);
        await _restaurantRepository.SaveChangesAsync(ct);
    }

    public async Task<TestRestaurantAiPhotoConfigResultDto> TestByRestaurantIdAsync(
        int restaurantId,
        TestRestaurantAiPhotoConfigRequestDto? req,
        CancellationToken ct)
    {
        var warnings = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        string? httpError = null;
        int? httpStatus = null;
        string? normalizedModelFinal = null;
        string? normalizedEndpointFinal = null;

        try
        {
            Restaurant? restaurant = null;
            try
            {
                restaurant = await GetRestaurantOrThrowAsync(restaurantId, ct);
            }
            catch
            {
                restaurant = null;
            }

            var apiKey = !string.IsNullOrWhiteSpace(req?.OverrideApiKey)
                ? req.OverrideApiKey!.Trim()
                : restaurant?.PhotoImportApiKey?.Trim();

            var rawEndpoint = !string.IsNullOrWhiteSpace(req?.OverrideEndpoint)
                ? req.OverrideEndpoint!.Trim()
                : restaurant?.PhotoImportApiEndpoint?.Trim();

            var rawModel = !string.IsNullOrWhiteSpace(req?.OverrideModel)
                ? req.OverrideModel!.Trim()
                : restaurant?.PhotoImportModel?.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new TestRestaurantAiPhotoConfigResultDto
                {
                    Success = false,
                    Provider = "Google Gemini",
                    NormalizedModel = null,
                    NormalizedEndpoint = null,
                    Warnings = warnings.Count > 0 ? warnings : null,
                    ErrorMessage = "Chave API não configurada.",
                    HttpStatusFromProvider = null,
                    LatencyMs = stopwatch.ElapsedMilliseconds
                };
            }

            if (string.IsNullOrWhiteSpace(rawEndpoint))
            {
                rawEndpoint = $"https://{RequiredEndpointHost}/v1beta";
                warnings.Add("Endpoint não configurado, usando padrão v1beta.");
            }

            if (!rawEndpoint.Contains(RequiredEndpointHost, StringComparison.OrdinalIgnoreCase))
            {
                return new TestRestaurantAiPhotoConfigResultDto
                {
                    Success = false,
                    Provider = "Google Gemini",
                    NormalizedModel = null,
                    NormalizedEndpoint = rawEndpoint,
                    Warnings = warnings.Count > 0 ? warnings : null,
                    ErrorMessage = $"Endpoint inválido: deve conter '{RequiredEndpointHost}'.",
                    HttpStatusFromProvider = null,
                    LatencyMs = stopwatch.ElapsedMilliseconds
                };
            }

            if (!rawEndpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                rawEndpoint = "https://" + rawEndpoint;
            }

            var normalizedEndpoint = rawEndpoint.TrimEnd('/');
            normalizedEndpointFinal = normalizedEndpoint;

            var normalizedModel = NormalizeModelName(rawModel);
            normalizedModelFinal = normalizedModel;

            if (normalizedModel != rawModel && !string.IsNullOrWhiteSpace(rawModel))
            {
                warnings.Add($"Nome do modelo normalizado de '{rawModel}' para '{normalizedModel}'.");
            }

            var url = $"{normalizedEndpoint}/models/{normalizedModel}:generateContent?key={Uri.EscapeDataString(apiKey!)}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "ping responda somente pong" }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds) };

            HttpResponseMessage? response = null;
            try
            {
                response = await httpClient.PostAsync(url, content, ct);
                httpStatus = (int)response.StatusCode;

                var responseJson = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    string? providerError = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(responseJson);
                        if (doc.RootElement.TryGetProperty("error", out var errorEl) &&
                            errorEl.TryGetProperty("message", out var msgEl))
                        {
                            providerError = msgEl.GetString();
                        }
                    }
                    catch
                    {
                    }

                    httpError = string.IsNullOrWhiteSpace(providerError)
                        ? $"Falha na requisição: HTTP {(int)response.StatusCode}."
                        : $"Erro do provedor: {providerError}";

                    return new TestRestaurantAiPhotoConfigResultDto
                    {
                        Success = false,
                        Provider = "Google Gemini",
                        NormalizedModel = normalizedModel,
                        NormalizedEndpoint = normalizedEndpoint,
                        Warnings = warnings.Count > 0 ? warnings : null,
                        ErrorMessage = httpError,
                        HttpStatusFromProvider = httpStatus,
                        LatencyMs = stopwatch.ElapsedMilliseconds
                    };
                }

                bool hasCandidates = false;
                try
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    hasCandidates = doc.RootElement.TryGetProperty("candidates", out var candidatesEl)
                                    && candidatesEl.ValueKind == JsonValueKind.Array
                                    && candidatesEl.GetArrayLength() > 0;
                }
                catch
                {
                    hasCandidates = false;
                }

                if (!hasCandidates)
                {
                    return new TestRestaurantAiPhotoConfigResultDto
                    {
                        Success = false,
                        Provider = "Google Gemini",
                        NormalizedModel = normalizedModel,
                        NormalizedEndpoint = normalizedEndpoint,
                        Warnings = warnings.Count > 0 ? warnings : null,
                        ErrorMessage = "Resposta inválida: sem candidates no JSON retornado.",
                        HttpStatusFromProvider = httpStatus,
                        LatencyMs = stopwatch.ElapsedMilliseconds
                    };
                }

                return new TestRestaurantAiPhotoConfigResultDto
                {
                    Success = true,
                    Provider = "Google Gemini",
                    NormalizedModel = normalizedModel,
                    NormalizedEndpoint = normalizedEndpoint,
                    Warnings = warnings.Count > 0 ? warnings : null,
                    ErrorMessage = null,
                    HttpStatusFromProvider = httpStatus,
                    LatencyMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (HttpRequestException ex)
            {
                httpError = $"Erro de conexão HTTP: {ex.Message}";
                if (ex.StatusCode.HasValue)
                {
                    httpStatus = (int)ex.StatusCode.Value;
                }
            }
            catch (TaskCanceledException)
            {
                httpError = $"Timeout após {RequestTimeoutSeconds}s aguardando resposta do provedor.";
            }
            catch (OperationCanceledException)
            {
                httpError = "Operação cancelada.";
            }
            catch (UriFormatException ex)
            {
                httpError = $"URL de endpoint malformada: {ex.Message}";
            }
            catch (JsonException ex)
            {
                httpError = $"Falha ao processar JSON de resposta: {ex.Message}";
            }
            finally
            {
                response?.Dispose();
            }
        }
        catch (Exception ex)
        {
            httpError = $"Falha inesperada: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
        }

        return new TestRestaurantAiPhotoConfigResultDto
        {
            Success = false,
            Provider = "Google Gemini",
            NormalizedModel = normalizedModelFinal,
            NormalizedEndpoint = normalizedEndpointFinal,
            Warnings = warnings.Count > 0 ? warnings : null,
            ErrorMessage = httpError,
            HttpStatusFromProvider = httpStatus,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<Restaurant> GetRestaurantOrThrowAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, ct);
        if (restaurant == null)
        {
            throw new InvalidOperationException($"Restaurante {restaurantId} não encontrado.");
        }
        return restaurant;
    }

    private static string NormalizeModelName(string? rawModel)
    {
        if (string.IsNullOrWhiteSpace(rawModel))
        {
            return DefaultModel;
        }

        var cleaned = Regex.Replace(rawModel, @"[^a-zA-Z0-9\-.]", string.Empty);
        cleaned = cleaned.Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? DefaultModel : cleaned;
    }
}
