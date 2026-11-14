using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ISM.Application.Interfaces.Integrations.IFood;
using ISM.Application.Options;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;


namespace ISM.Application.Services.Integrations.IFood;

public sealed class IFoodAuthClient : IIFoodAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IFoodOptions _options;
    
    public IFoodAuthClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IRestaurantRepository restaurantRepository,
        IOptions<IFoodOptions> options)
        {
        _httpClient = httpClient;
        _cache = cache;
        _restaurantRepository = restaurantRepository;
        _options = options.Value;
        }

    public async Task<string> GetAccessTokenAsync(int restaurantId, CancellationToken ct)
    {
        var cacheKey = $"ifood:token:{restaurantId}";
        if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached is not null)
            return cached;

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, ct)
            ?? throw new InvalidOperationException($"Restaurante {restaurantId} não encontrado");

        if (string.IsNullOrWhiteSpace(restaurant.IFoodClientId) ||
            string.IsNullOrWhiteSpace(restaurant.IFoodClientSecret))
            throw new InvalidOperationException("Credenciais do iFood não configuradas para este restaurante.");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grantType"] = "client_credentials",
            ["clientId"] = restaurant.IFoodClientId,
            ["clientSecret"] = restaurant.IFoodClientSecret
        });

        using var response = await _httpClient.PostAsync($"{_options.AuthBaseUrl}/oauth/token", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Falha ao autenticar com o iFood (HTTP {(int)response.StatusCode}): {body}");
        }
        
        var token = await response.Content.ReadFromJsonAsync<IFoodTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta de autenticação do iFood vazia ou inválida.");

        var ttl = TimeSpan.FromSeconds(Math.Max(1, token.ExpiresIn - _options.TokenCacheSafetyMarginSeconds));
        _cache.Set(cacheKey, token.AccessToken, ttl);

        return token.AccessToken;
    }
    
    public void InvalidateCachedToken(int restaurantId) =>
    _cache.Remove($"ifood:token:{restaurantId}");

    private sealed class IFoodTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }
}