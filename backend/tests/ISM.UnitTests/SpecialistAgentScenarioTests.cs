using System.Text.Json;
using FluentAssertions;
using ISM.Application.Agents;
using ISM.Application.Services.Agents;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Finance;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Orders;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Domain.Modules.Suppliers.Entities;

namespace ISM.UnitTests;

public sealed class SpecialistAgentScenarioTests
{
    public static IEnumerable<object[]> Scenarios()
    {
        for (var i = 1; i <= 10; i++) yield return ["stock-agir-" + i, "stock", JsonSerializer.SerializeToElement(new { productId = i }), AgentDecisionCode.CanAct];
        foreach (var scenario in new[] { new { name = "stock-no-input", json = "{}" }, new { name = "stock-zero-id", json = "{\"productId\":0}" }, new { name = "stock-unknown", json = "{\"productId\":999}" }, new { name = "stock-cross-tenant", json = "{\"productId\":998}" }, new { name = "stock-no-consumption", json = "{\"productId\":997}" }, new { name = "stock-negative-id", json = "{\"productId\":-1}" }, new { name = "stock-string-id", json = "{\"productId\":\"1\"}" }, new { name = "stock-array", json = "[]" }, new { name = "stock-null", json = "null" }, new { name = "stock-extra-invalid", json = "{\"productId\":0,\"restaurantId\":2}" }, new { name = "stock-unknown-2", json = "{\"productId\":1000}" }, new { name = "stock-unknown-3", json = "{\"productId\":1001}" }, new { name = "stock-no-consumption-2", json = "{\"productId\":996}" }, new { name = "stock-no-consumption-3", json = "{\"productId\":995}" }, new { name = "stock-boolean", json = "{\"productId\":true}" } })
            yield return [scenario.name, "stock", JsonDocument.Parse(scenario.json).RootElement.Clone(), scenario.name is "stock-cross-tenant" or "stock-extra-invalid" ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData];

        for (var i = 1; i <= 10; i++) yield return ["pricing-agir-" + i, "pricing", JsonSerializer.SerializeToElement(new { dishId = i }), AgentDecisionCode.CanAct];
        foreach (var scenario in new[] { new { name = "pricing-no-input", json = "{}" }, new { name = "pricing-zero-id", json = "{\"dishId\":0}" }, new { name = "pricing-unknown", json = "{\"dishId\":999}" }, new { name = "pricing-cross-tenant", json = "{\"dishId\":998}" }, new { name = "pricing-no-ingredients", json = "{\"dishId\":997}" }, new { name = "pricing-bad-margin-high", json = "{\"dishId\":1,\"targetMarginPercent\":100}" }, new { name = "pricing-bad-margin-low", json = "{\"dishId\":1,\"targetMarginPercent\":0}" }, new { name = "pricing-string-margin", json = "{\"dishId\":1,\"targetMarginPercent\":\"60\"}" }, new { name = "pricing-string-id", json = "{\"dishId\":\"1\"}" }, new { name = "pricing-negative-id", json = "{\"dishId\":-1}" }, new { name = "pricing-array", json = "[]" }, new { name = "pricing-null", json = "null" }, new { name = "pricing-unknown-2", json = "{\"dishId\":1000}" }, new { name = "pricing-unknown-3", json = "{\"dishId\":1001}" }, new { name = "pricing-extra-invalid", json = "{\"dishId\":0,\"restaurantId\":2}" } })
            yield return [scenario.name, "pricing", JsonDocument.Parse(scenario.json).RootElement.Clone(), scenario.name is "pricing-cross-tenant" or "pricing-extra-invalid" ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData];

        for (var i = 1; i <= 10; i++) yield return ["suppliers-agir-" + i, "suppliers", JsonSerializer.SerializeToElement(new { productName = "Arroz", unit = "kg", lookbackDays = 60 }), AgentDecisionCode.CanAct];
        foreach (var scenario in new[] { new { name = "suppliers-no-name", json = "{\"unit\":\"kg\"}" }, new { name = "suppliers-no-unit", json = "{\"productName\":\"Arroz\"}" }, new { name = "suppliers-empty-name", json = "{\"productName\":\" \",\"unit\":\"kg\"}" }, new { name = "suppliers-empty-unit", json = "{\"productName\":\"Arroz\",\"unit\":\" \"}" }, new { name = "suppliers-zero-lookback", json = "{\"productName\":\"Arroz\",\"unit\":\"kg\",\"lookbackDays\":0}" }, new { name = "suppliers-long-lookback", json = "{\"productName\":\"Arroz\",\"unit\":\"kg\",\"lookbackDays\":366}" }, new { name = "suppliers-wrong-product", json = "{\"productName\":\"Feijão\",\"unit\":\"kg\"}" }, new { name = "suppliers-wrong-unit", json = "{\"productName\":\"Arroz\",\"unit\":\"un\"}" }, new { name = "suppliers-string-days", json = "{\"productName\":\"Arroz\",\"unit\":\"kg\",\"lookbackDays\":\"60\"}" }, new { name = "suppliers-array", json = "[]" }, new { name = "suppliers-null", json = "null" }, new { name = "suppliers-negative-days", json = "{\"productName\":\"Arroz\",\"unit\":\"kg\",\"lookbackDays\":-1}" }, new { name = "suppliers-too-old", json = "{\"productName\":\"Arroz\",\"unit\":\"kg\",\"lookbackDays\":1}" }, new { name = "suppliers-extra-tenant", json = "{\"productName\":\"Arroz\",\"unit\":\"kg\",\"restaurantId\":2}" }, new { name = "suppliers-empty-object", json = "{}" } })
            yield return [scenario.name, "suppliers", JsonDocument.Parse(scenario.json).RootElement.Clone(), scenario.name == "suppliers-extra-tenant" ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData];

        for (var i = 1; i <= 10; i++) yield return ["finance-agir-" + i, "finance", JsonSerializer.SerializeToElement(new { startUtc = DateTime.UtcNow.AddDays(-3).ToString("O"), endUtc = DateTime.UtcNow.ToString("O") }), AgentDecisionCode.CanAct];
        foreach (var scenario in new[] { new { name = "finance-no-start", json = "{\"endUtc\":\"2030-01-02T00:00:00Z\"}" }, new { name = "finance-no-end", json = "{\"startUtc\":\"2030-01-01T00:00:00Z\"}" }, new { name = "finance-invalid-start", json = "{\"startUtc\":\"x\",\"endUtc\":\"2030-01-02T00:00:00Z\"}" }, new { name = "finance-invalid-end", json = "{\"startUtc\":\"2030-01-01T00:00:00Z\",\"endUtc\":\"x\"}" }, new { name = "finance-reversed", json = "{\"startUtc\":\"2030-01-02T00:00:00Z\",\"endUtc\":\"2030-01-01T00:00:00Z\"}" }, new { name = "finance-too-long", json = "{\"startUtc\":\"2020-01-01T00:00:00Z\",\"endUtc\":\"2022-01-01T00:00:00Z\"}" }, new { name = "finance-empty-future", json = "{\"startUtc\":\"2090-01-01T00:00:00Z\",\"endUtc\":\"2090-01-02T00:00:00Z\"}" }, new { name = "finance-array", json = "[]" }, new { name = "finance-null", json = "null" }, new { name = "finance-number-start", json = "{\"startUtc\":1,\"endUtc\":2}" }, new { name = "finance-same-date", json = "{\"startUtc\":\"2030-01-01T00:00:00Z\",\"endUtc\":\"2030-01-01T00:00:00Z\"}" }, new { name = "finance-extra-tenant", json = "{\"startUtc\":\"2030-01-01T00:00:00Z\",\"endUtc\":\"2030-01-02T00:00:00Z\",\"restaurantId\":2}" }, new { name = "finance-empty-object", json = "{}" }, new { name = "finance-bad-start-2", json = "{\"startUtc\":\"2020\",\"endUtc\":\"x\"}" }, new { name = "finance-bad-end-2", json = "{\"startUtc\":\"x\",\"endUtc\":\"2020\"}" } })
            yield return [scenario.name, "finance", JsonDocument.Parse(scenario.json).RootElement.Clone(), scenario.name == "finance-extra-tenant" ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task P2_specialists_execute_or_refuse_safely(string name, string specialist, JsonElement input, AgentDecisionCode expected)
    {
        var context = new AgentToolContext(7, 1, "Manager", "p2-" + name);
        AgentToolResult result = specialist switch
        {
            "stock" => await new StockReplenishmentTool(new ProductFake(), new MovementFake(name)).ExecuteAsync(input, context),
            "pricing" => await new DishPriceSimulationTool(new DishFake()).ExecuteAsync(input, context),
            "suppliers" => await new SupplierComparisonTool(new SupplierHistoryFake()).ExecuteAsync(input, context),
            "finance" => await new FinancePeriodTool(new FinanceFake(), new OrderFake()).ExecuteAsync(input, context),
            _ => throw new ArgumentOutOfRangeException(nameof(specialist))
        };
        result.Code.Should().Be(expected, name);
        result.Evidence.Should().NotContain(e => e.RestaurantId != context.RestaurantId, name);
    }

    private sealed class ProductFake : IProductRepository
    {
        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(id switch { 999 or 1000 or 1001 => null, 998 => new Product { Id = id, RestaurantId = 2 }, 996 or 995 => new Product { Id = id, RestaurantId = 1, CurrentQuantity = 10, MaximumQuantity = 20 }, _ => new Product { Id = id, RestaurantId = 1, Name = "Arroz", Unit = "kg", CurrentQuantity = 100, MaximumQuantity = 200 } });
        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Product>>([]);
        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(int? restaurantId, int pageNumber, int pageSize, string? search = null, bool? isCritical = null, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Product>, int)>(([], 0));
        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => Task.FromResult(product);
        public Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default) => Task.FromResult(product);
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class MovementFake(string scenario) : IStockMovementRepository
    {
        public Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(int restaurantId, int productId, DateTime? sinceUtc = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<StockMovement>>(scenario.Contains("no-consumption") ? [] : [new StockMovement { RestaurantId = restaurantId, ProductId = productId, MovementType = StockMovementType.ManualDecrease, QuantityDelta = -10, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) }]);
        public Task<StockMovement?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<StockMovement?>(null);
        public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetPagedByProductAsync(int restaurantId, int productId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<StockMovement>, int)>(([], 0));
        public Task<IReadOnlyDictionary<int, decimal>> SumNegativeDeltasLast30DaysAsync(int restaurantId, IEnumerable<int> productIds, DateTime sinceUtc, StockMovementType[] allowedTypes, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<int, decimal>>(new Dictionary<int, decimal>());
        public Task<StockMovement> AddAsync(StockMovement movement, CancellationToken cancellationToken = default) => Task.FromResult(movement);
        public Task BulkAddAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DishFake : IDishRepository
    {
        public Task<Dish?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Dish?>(id switch { 999 or 1000 or 1001 => null, 998 => new Dish { Id = id, RestaurantId = 2 }, 997 => new Dish { Id = id, RestaurantId = 1, Price = 10 }, _ => new Dish { Id = id, RestaurantId = 1, Name = "Prato", Price = 10, Ingredients = [new DishIngredient { Quantity = 2, Product = new Product { AverageCost = 2 } }] } });
        public Task<IReadOnlyCollection<Dish>> GetAllDishesAsync(int? restaurantId = null, int? categoryId = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Dish>>([]);
        public Task<Dish> AddDishAsync(Dish dish, CancellationToken cancellationToken = default) => Task.FromResult(dish);
        public Task<Dish?> UpdateDishAsync(int id, Dish dish, IReadOnlyCollection<DishIngredient> ingredients, CancellationToken cancellationToken = default) => Task.FromResult<Dish?>(dish);
        public Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyCollection<Dish>> GetDishesByProductIdAsync(int productId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Dish>>([]);
    }

    private sealed class SupplierHistoryFake : ISupplierProductPriceHistoryRepository
    {
        public Task<IReadOnlyList<SupplierProductPriceHistory>> GetForRestaurantSinceAsync(int restaurantId, DateTime sinceUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SupplierProductPriceHistory>>(new[] { new SupplierProductPriceHistory { RestaurantId = restaurantId, SupplierRawName = "Fornecedor A", ProductRawName = "Arroz", Unit = "kg", UnitPrice = 10, QuantityPurchased = 2, PurchasedAtUtc = DateTime.UtcNow.AddDays(-1) }, new SupplierProductPriceHistory { RestaurantId = restaurantId, SupplierRawName = "Fornecedor B", ProductRawName = "Arroz", Unit = "kg", UnitPrice = 12, QuantityPurchased = 1, PurchasedAtUtc = DateTime.UtcNow.AddDays(-2) } }.Where(row => row.PurchasedAtUtc >= sinceUtc).ToArray());
        public Task<SupplierProductPriceHistory?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<SupplierProductPriceHistory?>(null);
        public Task BulkAddAsync(IEnumerable<SupplierProductPriceHistory> history, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FinanceFake : IFinanceEntryRepository
    {
        public Task<IReadOnlyList<FinanceEntry>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FinanceEntry>>([new FinanceEntry { RestaurantId = restaurantId, Type = FinanceEntryType.Income, Amount = 100, SettledAtUtc = DateTime.UtcNow.AddDays(-1) }, new FinanceEntry { RestaurantId = restaurantId, Type = FinanceEntryType.Expense, Amount = 20, SettledAtUtc = DateTime.UtcNow.AddDays(-1) }]);
        public Task<IReadOnlyList<FinanceEntry>> GetPageAsync(int restaurantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FinanceEntry>>([]);
        public Task<int> CountAsync(int restaurantId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<FinanceEntry?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default) => Task.FromResult<FinanceEntry?>(null);
        public Task AddAsync(FinanceEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(FinanceEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class OrderFake : IOrderRepository
    {
        public Task<IReadOnlyList<Order>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Order>>([new Order { RestaurantId = restaurantId, Status = OrderStatus.Paid, PaidAtUtc = DateTime.UtcNow.AddDays(-1), Items = [new OrderItem { Quantity = 2, UnitPrice = 50 }] }]);
        public Task<Order?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<Order?> GetByRequestIdAsync(int restaurantId, Guid requestId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<IReadOnlyList<Order>> GetPageAsync(int restaurantId, int pageNumber, int pageSize, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Order>>([]);
        public Task<int> CountAsync(int restaurantId, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
