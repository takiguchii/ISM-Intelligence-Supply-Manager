using System.Net;
using FluentAssertions;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ISM.UnitTests;

public sealed class AssistantProviderResilienceTests
{
    [Fact]
    public async Task DisabledProvider_ReturnsHonestStatusAndCompletionFallback()
    {
        var provider = CreateProvider(new StubHandler(), enabled: false);

        var status = await provider.GetStatusAsync();
        var completion = await provider.CompleteAsync("system", "user");

        status.Enabled.Should().BeFalse();
        status.Reachable.Should().BeFalse();
        status.Error.Should().Contain("desabilitada");
        completion.Success.Should().BeFalse();
        completion.Error.Should().Be("Provider desabilitado");
    }

    [Fact]
    public async Task ProviderHttpFailure_ReturnsDegradedStatusAndCompletionWithoutThrowing()
    {
        var provider = CreateProvider(new StubHandler(HttpStatusCode.ServiceUnavailable));

        var status = await provider.GetStatusAsync();
        var completion = await provider.CompleteAsync("system", "user");

        status.Enabled.Should().BeTrue();
        status.Reachable.Should().BeFalse();
        status.Error.Should().Be("HTTP 503");
        completion.Success.Should().BeFalse();
        completion.Error.Should().Be("HTTP 503");
    }

    [Fact]
    public async Task ProviderTimeout_ReturnsTimeoutWithoutLeakingCancellation()
    {
        var provider = CreateProvider(new StubHandler(throwTimeout: true));

        var status = await provider.GetStatusAsync();
        var completion = await provider.CompleteAsync("system", "user");

        status.Error.Should().Be("Timeout");
        completion.Success.Should().BeFalse();
        completion.Error.Should().Be("Timeout");
    }

    private static OllamaTextProvider CreateProvider(HttpMessageHandler handler, bool enabled = true)
    {
        var client = new HttpClient(handler);
        var options = Options.Create(new AssistantAiOptions
        {
            Enabled = enabled,
            OllamaBaseUrl = "http://127.0.0.1:11434",
            Model = "gemma3:270m"
        });
        return new OllamaTextProvider(
            client,
            options,
            new TestCurrentUser(),
            null!,
            NullLogger<OllamaTextProvider>.Instance);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public int UserId => 1;
        public string? Email => "p6@test.local";
        public string Role => IsmRoles.Manager;
        public int? RestaurantId => null;
        public bool IsSuperAdmin => false;
        public bool IsManagerOrAbove => true;
    }

    private sealed class StubHandler(HttpStatusCode? statusCode = null, bool throwTimeout = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (throwTimeout)
                throw new TaskCanceledException("simulated provider timeout");

            return Task.FromResult(new HttpResponseMessage(statusCode ?? HttpStatusCode.OK)
            {
                Content = new StringContent("{\"models\":[{\"name\":\"gemma3:270m\"}]}")
            });
        }
    }
}
