using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ISM.Application.DTOs.Orders;
using ISM.Application.DTOs.Dashboard;
using ISM.Application.Security;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Orders;
using ISM.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.IntegrationTests;

[CollectionDefinition("TabPaymentTests", DisableParallelization = true)]
public sealed class TabPaymentCollection { }

[Collection("TabPaymentTests")]
public sealed class TabPaymentTests(FoundationWebApplicationFactory factory) : IClassFixture<FoundationWebApplicationFactory>
{
    private const int RestaurantId = 940;
    private const int DishId = 94010;

    [Fact]
    public async Task CashierCanConfirmPaymentForAllCompletedOrdersAtTheTable()
    {
        var (table, firstOrder) = await CreateCompletedOrder();
        var (_, secondOrder) = await CreateCompletedOrder(table);
        using var cashier = Client(IsmRoles.Cashier);
        using var waiter = Client(IsmRoles.Waiter);

        var tab = await OpenTab(cashier, table);
        Assert.Equal(80m, tab.Total);
        Assert.Equal(new[] { firstOrder, secondOrder }, tab.OrderIds.OrderBy(id => id));
        var payment = new Payment(Guid.NewGuid(), PaymentMethod.Pix, 80m);
        Assert.Equal(HttpStatusCode.Forbidden, (await Pay(waiter, tab.Id, payment)).StatusCode);
        var receipt = await ReadReceipt(await Pay(cashier, tab.Id, payment));
        Assert.Equal(tab.Id, receipt.TabId);
        Assert.Equal(payment.Total, receipt.Total);
        Assert.Equal(payment.PaymentMethod, receipt.PaymentMethod);
        Assert.Equal(HttpStatusCode.NotFound, (await cashier.GetAsync(OpenTabUrl(table))).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        foreach (var id in tab.OrderIds)
        {
            var order = await db.Orders.FindAsync(id);
            Assert.Equal(OrderStatus.Paid, order!.Status);
            Assert.Equal(PaymentMethod.Pix, order.PaymentMethod);
            Assert.Equal(receipt.PaidAtUtc, order.PaidAtUtc);
            Assert.Equal(4, order.Version);
        }
    }

    [Fact]
    public async Task OpenTabsQueueReturnsOneTabForTwoOrdersAtTheSameTable()
    {
        var (table, firstOrder) = await CreateCompletedOrder();
        var (_, secondOrder) = await CreateCompletedOrder(table);
        using var cashier = Client(IsmRoles.Cashier);

        var response = await cashier.GetAsync("/api/orders/tabs/open");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tabs = (await response.Content.ReadFromJsonAsync<OpenTabView[]>())!;
        var tab = Assert.Single(tabs, candidate => candidate.OrderIds.Contains(firstOrder) && candidate.OrderIds.Contains(secondOrder));
        Assert.Equal(80m, tab.Total);
        Assert.Equal(new[] { firstOrder, secondOrder }, tab.OrderIds.OrderBy(id => id));
    }

    [Fact]
    public async Task OnePaymentSettlesTwoRoundsWithTheCombinedTotal()
    {
        using var manager = Client(IsmRoles.Manager);
        var before = await manager.GetFromJsonAsync<DashboardResponse>("/api/dashboard/metrics");
        var (table, firstOrder) = await CreateCompletedOrder(price: 106.90m);
        var (_, secondOrder) = await CreateCompletedOrder(table, price: 135.90m);
        using var cashier = Client(IsmRoles.Cashier);
        var tab = await OpenTab(cashier, table);
        Assert.Equal(242.80m, tab.Total);

        var receipt = await ReadReceipt(await Pay(cashier, tab.Id, new Payment(Guid.NewGuid(), PaymentMethod.Pix, 242.80m)));
        Assert.Equal(242.80m, receipt.Total);
        Assert.Equal(HttpStatusCode.NotFound, (await cashier.GetAsync(OpenTabUrl(table))).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        Assert.All(new[] { firstOrder, secondOrder }, id => Assert.Equal(OrderStatus.Paid, db.Orders.Find(id)!.Status));
        var after = await manager.GetFromJsonAsync<DashboardResponse>("/api/dashboard/metrics");
        Assert.Equal(242.80m, after!.RevenueVsCost.Sum(point => point.Receita) - before!.RevenueVsCost.Sum(point => point.Receita));
    }

    [Fact]
    public async Task SameKeyReturnsTheSameReceiptAndRejectsDifferentPayload()
    {
        var (table, _) = await CreateCompletedOrder();
        using var cashier = Client(IsmRoles.Cashier);
        var tab = await OpenTab(cashier, table);
        var payment = new Payment(Guid.NewGuid(), PaymentMethod.Pix, tab.Total);

        var first = await ReadReceipt(await Pay(cashier, tab.Id, payment));
        Assert.Equal(first, await ReadReceipt(await Pay(cashier, tab.Id, payment)));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Pay(cashier, tab.Id, payment with { PaymentMethod = PaymentMethod.Cash })).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentRequestsSettleOnlyOnce(bool sameKey)
    {
        var (table, orderId) = await CreateCompletedOrder();
        using var cashier = Client(IsmRoles.Cashier);
        using var secondCashier = Client(IsmRoles.Cashier);
        var tab = await OpenTab(cashier, table);
        var payment = new Payment(Guid.NewGuid(), PaymentMethod.Pix, tab.Total);
        var other = sameKey ? payment : payment with { IdempotencyKey = Guid.NewGuid() };
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpResponseMessage> Send(HttpClient client, Payment request)
        {
            await start.Task;
            return await Pay(client, tab.Id, request);
        }

        var first = Send(cashier, payment);
        var second = Send(secondCashier, other);
        start.SetResult();
        var responses = await Task.WhenAll(first, second);

        if (sameKey)
            Assert.Equal(await ReadReceipt(responses[0]), await ReadReceipt(responses[1]));
        else
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        }
        var order = await cashier.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}");
        Assert.Equal(OrderStatus.Paid, order!.Status);
        Assert.Equal(4, order.Version);
    }

    [Theory]
    [InlineData(3999, PaymentMethod.Pix)]
    [InlineData(4001, PaymentMethod.Pix)]
    [InlineData(4000, PaymentMethod.Unspecified)]
    [InlineData(4000, (PaymentMethod)99)]
    public async Task InvalidPaymentLeavesTheTabAndOrderUnchanged(int cents, PaymentMethod method)
    {
        var (table, orderId) = await CreateCompletedOrder();
        using var cashier = Client(IsmRoles.Cashier);
        var tab = await OpenTab(cashier, table);
        var payment = new Payment(Guid.NewGuid(), method, cents / 100m);

        Assert.Equal(HttpStatusCode.BadRequest, (await Pay(cashier, tab.Id, payment)).StatusCode);
        Assert.Equal(tab.Id, (await OpenTab(cashier, table)).Id);
        var order = await cashier.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}");
        Assert.Equal(OrderStatus.Completed, order!.Status);
        Assert.Null(order.PaidAtUtc);
        await ReadReceipt(await Pay(cashier, tab.Id, payment with { PaymentMethod = PaymentMethod.Pix, Total = tab.Total }));
    }

    [Fact]
    public async Task ClosedTabCannotBePaidAgainAndNextOrderUsesANewTab()
    {
        var (table, _) = await CreateCompletedOrder();
        using var cashier = Client(IsmRoles.Cashier);
        var firstTab = await OpenTab(cashier, table);
        var payment = new Payment(Guid.NewGuid(), PaymentMethod.Pix, firstTab.Total);
        await ReadReceipt(await Pay(cashier, firstTab.Id, payment));

        var (_, nextOrder) = await CreateCompletedOrder(table);
        var nextTab = await OpenTab(cashier, table);
        Assert.NotEqual(firstTab.Id, nextTab.Id);
        Assert.Equal(nextOrder, Assert.Single(nextTab.OrderIds));
        Assert.Equal(HttpStatusCode.Conflict,
            (await Pay(cashier, firstTab.Id, payment with { IdempotencyKey = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Pay(cashier, nextTab.Id, payment)).StatusCode);
        await ReadReceipt(await Pay(cashier, nextTab.Id, payment with { IdempotencyKey = Guid.NewGuid() }));
    }

    [Fact]
    public async Task RestaurantsWithTheSameTableCanUseTheSameKeyIndependently()
    {
        var (table, _) = await CreateCompletedOrder();
        using var cashier = Client(IsmRoles.Cashier);
        using var foreignCashier = Client(IsmRoles.Cashier, RestaurantId + 1);
        var original = await OpenTab(cashier, table);
        var payment = new Payment(Guid.NewGuid(), PaymentMethod.Cash, original.Total);

        Assert.Equal(HttpStatusCode.NotFound, (await foreignCashier.GetAsync(OpenTabUrl(table))).StatusCode);
        await CreateCompletedOrder(table, RestaurantId + 1);
        var foreign = await OpenTab(foreignCashier, table);
        Assert.NotEqual(original.Id, foreign.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await Pay(foreignCashier, original.Id, payment)).StatusCode);
        await ReadReceipt(await Pay(cashier, original.Id, payment));
        Assert.Equal(HttpStatusCode.NotFound, (await Pay(foreignCashier, original.Id, payment)).StatusCode);
        await ReadReceipt(await Pay(foreignCashier, foreign.Id, payment));
    }

    private async Task<(string Table, int OrderId)> CreateCompletedOrder(string? table = null, int restaurantId = RestaurantId, decimal? price = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        var dishId = price.HasValue ? DishId + 1000 + Interlocked.Increment(ref _pricedDishSeed) : DishId + restaurantId - RestaurantId;
        if (await db.Dishes.FindAsync(dishId) is null)
        {
            db.Dishes.Add(new Dish { Id = dishId, RestaurantId = restaurantId, CategoryId = 1, Name = "Prato", Price = price ?? 40m, IsActive = true });
            await db.SaveChangesAsync();
        }

        table ??= $"Mesa {Guid.NewGuid():N}";
        using var waiter = Client(IsmRoles.Waiter, restaurantId);
        using var chef = Client(IsmRoles.Chef, restaurantId);
        var created = await waiter.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            RequestId = Guid.NewGuid(),
            TableReference = table,
            Items = [new() { DishId = dishId, Quantity = 1 }]
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = (await created.Content.ReadFromJsonAsync<OrderResponse>())!;
        var statusUrl = $"/api/orders/{order.Id}/status";
        Assert.Equal(HttpStatusCode.OK, (await chef.PutAsJsonAsync(statusUrl, new UpdateOrderStatusRequest(OrderStatus.Preparing, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await chef.PutAsJsonAsync(statusUrl, new UpdateOrderStatusRequest(OrderStatus.Ready, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await waiter.PutAsJsonAsync(statusUrl, new UpdateOrderStatusRequest(OrderStatus.Completed, 2))).StatusCode);
        return (table, order.Id);
    }

    private static int _pricedDishSeed;

    private HttpClient Client(string role, int restaurantId = RestaurantId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(role, restaurantId));
        return client;
    }

    private static string OpenTabUrl(string table) => $"/api/orders/tabs/open?tableReference={Uri.EscapeDataString(table)}";

    private static async Task<OpenTabView> OpenTab(HttpClient client, string table)
    {
        var response = await client.GetAsync(OpenTabUrl(table));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OpenTabView>())!;
    }

    private static Task<HttpResponseMessage> Pay(HttpClient client, int tabId, Payment payment) =>
        client.PostAsJsonAsync($"/api/orders/tabs/{tabId}/payments", payment);

    private static async Task<Receipt> ReadReceipt(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Receipt>())!;
    }

    private sealed record OpenTabView(int Id, decimal Total, int[] OrderIds);
    private sealed record Payment(Guid IdempotencyKey, PaymentMethod PaymentMethod, decimal Total);
    private sealed record Receipt(int TabId, PaymentMethod PaymentMethod, decimal Total, DateTime PaidAtUtc);
}
