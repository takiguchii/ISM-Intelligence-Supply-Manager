using System.Net;
using System.Text;
using ISM.Application.Options;
using ISM.Application.Services.Integrations.IFood;
using ISM.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ISM.UnitTests;

/// <summary>Handler fake — simula as respostas do sandbox do iFood sem chamada de rede real.</summary>
internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(respond(request));
    }
}

public class IFoodAuthClientTests
{
    private static Restaurant NewRestaurantComCredenciais(int id = 1) => new()
    {
        Id = id,
        Name = "Restaurante Teste",
        CNPJ = "12345678901234",
        Created = DateTime.UtcNow,
        IFoodClientId = "client-id",
        IFoodClientSecret = "client-secret",
    };

    private static IFoodAuthClient CreateClient(FakeRestaurantRepository repo, HttpMessageHandler handler) =>
        new(new HttpClient(handler), new MemoryCache(new MemoryCacheOptions()), repo,
            Options.Create(new IFoodOptions
            {
                AuthBaseUrl = "https://sandbox.ifood.test",
                ApiBaseUrl = "https://sandbox.ifood.test",
            }));

    private static HttpResponseMessage OkTokenResponse(string token) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            "{\"accessToken\":\"" + token + "\",\"expiresIn\":21600}",
            Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetAccessTokenAsync_ComCredenciaisValidas_DeveRetornarTokenDaResposta()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurantComCredenciais());
        var handler = new FakeHttpMessageHandler(_ => OkTokenResponse("tok-123"));
        var client = CreateClient(repo, handler);

        var token = await client.GetAccessTokenAsync(1, CancellationToken.None);

        Assert.Equal("tok-123", token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RestauranteSemCredenciais_DeveLancarInvalidOperationException()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(new Restaurant { Id = 1, Name = "R", CNPJ = "12345678901234", Created = DateTime.UtcNow });
        var handler = new FakeHttpMessageHandler(_ => OkTokenResponse("nao-deveria-chegar-aqui"));
        var client = CreateClient(repo, handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAccessTokenAsync(1, CancellationToken.None));

        Assert.Contains("Credenciais", ex.Message);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RespostaDeErro_DeveLancarComMensagemContendoAutenticar()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurantComCredenciais());
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("invalid_client", Encoding.UTF8, "text/plain")
        });
        var client = CreateClient(repo, handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAccessTokenAsync(1, CancellationToken.None));

        Assert.Contains("Falha ao autenticar com o iFood", ex.Message);
        Assert.Contains("invalid_client", ex.Message);
    }

    [Fact]
    public async Task GetAccessTokenAsync_SegundaChamada_DeveUsarTokenEmCacheSemNovaChamadaHttp()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurantComCredenciais());
        var handler = new FakeHttpMessageHandler(_ => OkTokenResponse("tok-cache"));
        var client = CreateClient(repo, handler);

        var first = await client.GetAccessTokenAsync(1, CancellationToken.None);
        var second = await client.GetAccessTokenAsync(1, CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task InvalidateCachedToken_DeveForcarNovaChamadaHttpNaProximaVez()
    {
        var repo = new FakeRestaurantRepository();
        repo.Seed(NewRestaurantComCredenciais());
        var callNumber = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callNumber++;
            return OkTokenResponse($"tok-{callNumber}");
        });
        var client = CreateClient(repo, handler);

        var first = await client.GetAccessTokenAsync(1, CancellationToken.None);
        client.InvalidateCachedToken(1);
        var second = await client.GetAccessTokenAsync(1, CancellationToken.None);

        Assert.NotEqual(first, second);
        Assert.Equal(2, handler.CallCount);
    }
}
