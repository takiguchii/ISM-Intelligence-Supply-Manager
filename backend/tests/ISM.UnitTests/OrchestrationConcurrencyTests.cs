using System.Text.Json;
using FluentAssertions;
using ISM.Application.Agents;
using ISM.Application.Services.Agents;
using ISM.Application.Services.Assistant;

namespace ISM.UnitTests;

public sealed class OrchestrationConcurrencyTests
{
    [Fact]
    public async Task Twenty_independent_queries_keep_results_scoped_and_complete()
    {
        var registry = new ConcurrentRegistry();
        var service = new ConstrainedOrchestrationService(registry, new AgentIntentRouter());
        var plans = await Task.WhenAll(Enumerable.Range(1, 20).Select(id =>
            service.PlanAsync(new($"faturamento e saldo do produto {id}", id, id % 2 == 0 ? 2 : 1, "Manager"))));

        var results = await Task.WhenAll(plans.Select(plan => service.ExecuteAsync(plan)));

        results.Should().HaveCount(20);
        results.Should().OnlyContain(result => !result.Partial && result.Steps.Count == 2);
        results.SelectMany(result => result.Evidence).Should().OnlyContain(evidence => IsExpectedRestaurant(evidence.RestaurantId));
        registry.Calls.Should().Be(40);
    }

    private sealed class ConcurrentRegistry : IAgentToolRegistry
    {
        private int _calls;
        public int Calls => _calls;
        public IReadOnlyCollection<AgentToolDescriptor> List() =>
        [
            new("finance.getRevenueSummary", "finance", "1", "", AgentToolOperation.Read, ["Manager"], 1, 3000, false, "low"),
            new("stock.getProduct", "stock", "1", "", AgentToolOperation.Read, ["Manager"], 1, 3000, false, "low")
        ];

        public async Task<AgentToolResult> ExecuteAsync(AgentToolExecuteRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            await Task.Delay(1, cancellationToken);
            var restaurantId = request.Input.TryGetProperty("productId", out var product) && product.TryGetInt32(out var id) ? id % 2 == 0 ? 2 : 1 : 1;
            return new(request.ToolName, AgentDecisionCode.CanAct, new { ok = true }, [new("Fixture", request.ToolName, restaurantId, DateTime.UtcNow, null, "fixture")], "ok", request.CorrelationId ?? "corr", 1);
        }
    }

    private static bool IsExpectedRestaurant(int restaurantId) => restaurantId is 1 or 2;
}
