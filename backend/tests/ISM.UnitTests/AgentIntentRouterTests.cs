using System.Text.Json;
using FluentAssertions;
using ISM.Application.Services.Agents;

namespace ISM.UnitTests;

public sealed class AgentIntentRouterTests
{
    private readonly AgentIntentRouter _router = new();

    [Fact]
    public void ResolveStructured_AcceptsOnlyAllowlistedSchema()
    {
        using var document = JsonDocument.Parse("""
            {"specialist":"stock","toolName":"stock.getProduct","arguments":{"productId":42},"confidence":0.9}
            """);

        var intent = _router.ResolveStructured(document.RootElement);

        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be("stock.getProduct");
        intent.Confidence.Should().Be(0.9);
        intent.Input.GetProperty("productId").GetInt32().Should().Be(42);
    }

    [Theory]
    [InlineData("{\"specialist\":\"stock\",\"toolName\":\"unknown.tool\",\"arguments\":{}}")]
    [InlineData("{\"specialist\":\"stock\",\"toolName\":\"stock.getProduct\",\"arguments\":{\"productId\":42,\"restaurantId\":7}}")]
    [InlineData("{\"specialist\":\"finance\",\"toolName\":\"finance.getRevenueSummary\",\"arguments\":{\"extra\":true}}")]
    public void ResolveStructured_RejectsUnknownOrUnsafeSchema(string json)
    {
        using var document = JsonDocument.Parse(json);

        _router.ResolveStructured(document.RootElement).Should().BeNull();
    }

    [Fact]
    public void Resolve_ProvidesDeterministicSafeFallback()
    {
        var intent = _router.Resolve("consulte o saldo do produto 42");
        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be("stock.getProduct");
        intent.Input.GetProperty("productId").GetInt32().Should().Be(42);
        _router.Resolve("faça algo criativo").Should().BeNull();
    }
}
