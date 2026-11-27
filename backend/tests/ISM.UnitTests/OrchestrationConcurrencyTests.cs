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
        registry.MaxConcurrentStepsWithinQuery.Should().Be(1);
    }

    private sealed class ConcurrentRegistry : IAgentToolRegistry
    {
        private int _calls;
        private int _maxConcurrentStepsWithinQuery;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _activeByCorrelation = new();
        public int Calls => _calls;
        public int MaxConcurrentStepsWithinQuery => _maxConcurrentStepsWithinQuery;
        public IReadOnlyCollection<AgentToolDescriptor> List() =>
        [
            new("finance.getRevenueSummary", "finance", "1", "", AgentToolOperation.Read, ["Manager"], 1, 3000, false, "low"),
            new("stock.getProduct", "stock", "1", "", AgentToolOperation.Read, ["Manager"], 1, 3000, false, "low")
        ];

        public async Task<AgentToolResult> ExecuteAsync(AgentToolExecuteRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            var correlationId = request.CorrelationId ?? string.Empty;
            var active = _activeByCorrelation.AddOrUpdate(correlationId, 1, (_, current) => current + 1);
            UpdateMaximum(active);
            try
            {
                await Task.Delay(1, cancellationToken);
                var restaurantId = request.Input.TryGetProperty("productId", out var product) && product.TryGetInt32(out var id) ? id % 2 == 0 ? 2 : 1 : 1;
                object data = request.ToolName switch
                {
                    "finance.getRevenueSummary" => new { SalesRevenue = 100m, OtherIncome = 0m, Expenses = 0m, AccountsReceivable = 0m, AccountsPayable = 0m },
                    "stock.getProduct" => new { Name = $"Produto {restaurantId}", CurrentQuantity = 10m, MinimumQuantity = 2m, AverageCost = 5m, IsCritical = false },
                    _ => new { }
                };
                return new(request.ToolName, AgentDecisionCode.CanAct, data, [new("Fixture", request.ToolName, restaurantId, DateTime.UtcNow, null, "fixture")], "ok", request.CorrelationId ?? "corr", 1);
            }
            finally
            {
                _activeByCorrelation.AddOrUpdate(correlationId, 0, (_, current) => current - 1);
            }
        }

        private void UpdateMaximum(int value)
        {
            var current = Volatile.Read(ref _maxConcurrentStepsWithinQuery);
            while (value > current)
            {
                var observed = Interlocked.CompareExchange(ref _maxConcurrentStepsWithinQuery, value, current);
                if (observed == current) return;
                current = observed;
            }
        }
    }

    private static bool IsExpectedRestaurant(int restaurantId) => restaurantId is 1 or 2;
}
