using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ISM.Application.Interfaces.Integrations.IFood;
using ISM.Application.Options;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Options;


namespace ISM.Application.Services.Integrations.IFood;

public sealed class IFoodCatalogClient : IIFoodCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly IIFoodAuthClient _authClient;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IFoodOptions  _options;

    public IFoodCatalogClient(
        HttpClient httpClient,
        IIFoodAuthClient authClient,
        IRestaurantRepository restaurantRepository,
        IOptions<IFoodOptions> options)
    {
        _httpClient = httpClient;
        _authClient = authClient;
        _restaurantRepository = restaurantRepository;
        _options = options.Value;
    }

    public async Task<string> UpsertCategoryAsync(int restaurantId, string name, CancellationToken ct)
    {
        var merchantId = await GetMerchantIdOrThrowAsync(restaurantId, ct);
        var catalogId = await GetCatalogIdAsync(restaurantId, merchantId, ct);
        var categoryId = Guid.NewGuid().ToString();

        var payload = new
        {
            id = categoryId,
            name,
            status = "AVAILABLE",
            index = 0,
            template = "DEFAULT"
        };

        using var _ = await SendAsync(restaurantId, HttpMethod.Post, $"/catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories", payload, ct);
        return categoryId;
    }

    public async Task<string> UpsertItemAsync(int restaurantId, string ifoodCategoryId, string name, string description, decimal price, CancellationToken ct)
    {
        var merchantId = await GetMerchantIdOrThrowAsync(restaurantId, ct);
        var itemId = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid().ToString();

        var payload = new
        {
            item = new
            {
                id = itemId,
                type = "DEFAULT",
                categoryId = ifoodCategoryId,
                status = "AVAILABLE",
                price = new { value = price, originalValue = price },
                index = 0,
                productId
            },
            products = new[]
            {
                new { id = productId, name, description }
            }
        };

        using var _ = await SendAsync(restaurantId, HttpMethod.Put, $"/catalog/v2.0/merchants/{merchantId}/items", payload, ct);
        return itemId;
    }

    public async Task SetItemAvailabilityAsync(int restaurantId, string ifoodItemId, bool isAvailable, CancellationToken ct)
    {
        var merchantId = await GetMerchantIdOrThrowAsync(restaurantId, ct);
        var payload = new { status = isAvailable ? "AVAILABLE" : "UNAVAILABLE" };
        using var _ = await SendAsync(restaurantId, HttpMethod.Patch, $"/catalog/v2.0/merchants/{merchantId}/items/{ifoodItemId}", payload, ct);
    }

    private async Task<string> GetCatalogIdAsync(int restaurantId, string merchantId, CancellationToken ct)
    {
        using var response = await SendAsync(restaurantId, HttpMethod.Get, $"/catalog/v2.0/merchants/{merchantId}/catalogs", null, ct);
        var catalogs = await response.Content.ReadFromJsonAsync<List<IFoodCatalogResponse>>(cancellationToken: ct);
        return catalogs?.FirstOrDefault()?.CatalogId
            ?? throw new InvalidOperationException("Nenhum catálogo encontrado para este merchant no iFood.");
    }

    private async Task<HttpResponseMessage> SendAsync(int restaurantId, HttpMethod method, string path, object? payload, CancellationToken ct)
    {
        var token = await _authClient.GetAccessTokenAsync(restaurantId, ct);
        using var request = new HttpRequestMessage(method, $"{_options.ApiBaseUrl}{path}");
        if (payload is not null)
            request.Content = JsonContent.Create(payload);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Chamada ao catálogo do iFood falhou (HTTP {(int)response.StatusCode}) em {path}: {body}");
        }
        return response;
    }

    private async Task<string> GetMerchantIdOrThrowAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, ct);
        return restaurant?.IFoodMerchantId
               ?? throw new InvalidOperationException("MerchantId do iFood não configurado para este restaurante.");
    }

    private sealed class IFoodCatalogResponse
    {
        [JsonPropertyName("catalogId")]
        public string CatalogId { get; set; } = string.Empty;
    }
}