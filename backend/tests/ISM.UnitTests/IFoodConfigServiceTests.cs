using ISM.Application.DTOs.Restaurant;
using ISM.Application.Interfaces.Integrations.IFood;
using ISM.Application.Services.Restaurant;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.UnitTests;

/// <summary>Repositório fake em memória — simula o EF sem precisar de MySQL/Docker.</summary>
internal sealed class FakeRestaurantRepository : IRestaurantRepository
{
    private readonly Dictionary<int, Restaurant> _restaurants = new();

    public int UpdateCalls { get; private set; }
    public int SaveChangesCalls { get; private set; }

    public void Seed(Restaurant restaurant) => _restaurants[restaurant.Id] = restaurant;

    public Task<Restaurant?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_restaurants.GetValueOrDefault(id));

    public Task<IReadOnlyList<Restaurant>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Restaurant>>(_restaurants.Values.ToList());

    public Task AddRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        _restaurants[restaurant.Id] = restaurant;
        return Task.CompletedTask;
    }

    public void Update(Restaurant restaurant) => UpdateCalls++;

    public void Delete(Restaurant restaurant) => _restaurants.Remove(restaurant.Id);

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.FromResult(true);
    }

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
}

/// <summary>Auth client fake — simula o iFood sandbox sem chamada HTTP real.</summary>
internal sealed class FakeIFoodAuthClient : IIFoodAuthClient
{
    public string TokenToReturn { get; set; } = "fake-token";
    public Exception? ExceptionToThrow { get; set; }
    public List<int> InvalidatedRestaurantIds { get; } = [];

    public Task<string> GetAccessTokenAsync(int restaurantId, CancellationToken ct)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult(TokenToReturn);
    }

    public void InvalidateCachedToken(int restaurantId) => InvalidatedRestaurantIds.Add(restaurantId);
}

public class IFoodConfigServiceTests
{
    private static Restaurant NewRestaurant(int id = 1) => new()
    {
        Id = id,
        Name = "Restaurante Teste",
        CNPJ = "12345678901234",
        Created = DateTime.UtcNow,
    };

    [Fact]
    public async Task GetMyConfigAsync_SemCredenciais_DeveRetornarHasCredentialsFalse()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurant());
        var service = new IFoodConfigService(repo, new FakeIFoodAuthClient());

        var dto = await service.GetMyConfigAsync(1, CancellationToken.None);

        Assert.False(dto.HasCredentials);
        Assert.Null(dto.ClientIdLast4);
        Assert.Null(dto.MerchantId);
    }

    [Fact]
    public async Task GetMyConfigAsync_ComCredenciais_DeveRetornarUltimos4DigitosDoClientId()
    {
        var repo = new FakeRestaurantRepository();
        var restaurant = NewRestaurant();
        restaurant.IFoodClientId = "abcd1234WXYZ";
        restaurant.IFoodClientSecret = "segredo";
        restaurant.IFoodMerchantId = "merchant-9";
        repo.Seed(restaurant);
        var service = new IFoodConfigService(repo, new FakeIFoodAuthClient());

        var dto = await service.GetMyConfigAsync(1, CancellationToken.None);

        Assert.True(dto.HasCredentials);
        Assert.Equal("WXYZ", dto.ClientIdLast4);
        Assert.Equal("merchant-9", dto.MerchantId);
    }

    [Fact]
    public async Task GetMyConfigAsync_RestauranteNaoEncontrado_DeveLancarInvalidOperationException()
    {
        var repo = new FakeRestaurantRepository();
        var service = new IFoodConfigService(repo, new FakeIFoodAuthClient());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetMyConfigAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMyConfigAsync_DeveGravarCredenciaisEInvalidarCacheDoToken()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurant());
        var authClient = new FakeIFoodAuthClient();
        var service = new IFoodConfigService(repo, authClient);

        await service.UpdateMyConfigAsync(1, new UpdateIFoodConfigDto
        {
            ClientId = "  novo-client-id  ",
            ClientSecret = "  novo-secret  ",
            MerchantId = "  merchant-1  ",
        }, CancellationToken.None);

        var dto = await service.GetMyConfigAsync(1, CancellationToken.None);
        Assert.True(dto.HasCredentials);
        Assert.Equal("merchant-1", dto.MerchantId);
        Assert.Contains(1, authClient.InvalidatedRestaurantIds);
        Assert.Equal(1, repo.UpdateCalls);
        Assert.Equal(1, repo.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateMyConfigAsync_ClearCredentials_DeveLimparOsTresCampos()
    {
        var repo = new FakeRestaurantRepository();
        var restaurant = NewRestaurant();
        restaurant.IFoodClientId = "x";
        restaurant.IFoodClientSecret = "y";
        restaurant.IFoodMerchantId = "z";
        repo.Seed(restaurant);
        var service = new IFoodConfigService(repo, new FakeIFoodAuthClient());

        await service.UpdateMyConfigAsync(1, new UpdateIFoodConfigDto { ClearCredentials = true }, CancellationToken.None);

        var dto = await service.GetMyConfigAsync(1, CancellationToken.None);
        Assert.False(dto.HasCredentials);
        Assert.Null(dto.MerchantId);
    }

    [Fact]
    public async Task UpdateMyConfigAsync_RestauranteNaoEncontrado_DeveLancarInvalidOperationException()
    {
        var repo = new FakeRestaurantRepository();
        var service = new IFoodConfigService(repo, new FakeIFoodAuthClient());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateMyConfigAsync(999, new UpdateIFoodConfigDto(), CancellationToken.None));
    }

    [Fact]
    public async Task TestMyConfigAsync_QuandoAutenticaComSucesso_DeveRetornarSuccessTrue()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurant());
        var authClient = new FakeIFoodAuthClient { TokenToReturn = "abc123" };
        var service = new IFoodConfigService(repo, authClient);

        var result = await service.TestMyConfigAsync(1, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.TokenObtained);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task TestMyConfigAsync_QuandoAutenticacaoFalha_DeveRetornarSuccessFalseComMensagemDeErro()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurant());
        var authClient = new FakeIFoodAuthClient
        {
            ExceptionToThrow = new InvalidOperationException("Falha ao autenticar com o iFood (HTTP 401): invalid_client")
        };
        var service = new IFoodConfigService(repo, authClient);

        var result = await service.TestMyConfigAsync(1, CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(result.TokenObtained);
        Assert.Contains("invalid_client", result.ErrorMessage);
    }
}
