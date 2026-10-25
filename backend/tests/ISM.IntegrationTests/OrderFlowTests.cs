using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ISM.Application.DTOs.Orders;
using ISM.Application.DTOs.Dashboard;
using ISM.Application.Security;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Orders;
using ISM.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.IntegrationTests;

public sealed class OrderFlowTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;
    public OrderFlowTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateAndAdvanceOrder_UsesSnapshots_IsIdempotent_AndIsolatesRestaurants()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        db.Dishes.Add(new Dish { Id = 90010, RestaurantId = 900, CategoryId = 1, Name = "Prato de teste", Price = 40, Cost = 15, IsActive = true });
        await db.SaveChangesAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Waiter, 900));
        var request = new CreateOrderRequest { RequestId = Guid.NewGuid(), TableReference = "Mesa 5", Items = [new() { DishId = 90010, Quantity = 2, Observation = "sem sal" }] };

        var response = await client.PostAsJsonAsync("/api/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
        created.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(40);
        created.Items.Single().UnitCost.Should().Be(15);
        created.Status.Should().Be(OrderStatus.Submitted);
        created.PaidAtUtc.Should().BeNull();
        var retry = await client.PostAsJsonAsync("/api/orders", request);
        (await retry.Content.ReadFromJsonAsync<OrderResponse>())!.Id.Should().Be(created.Id);
        request.Items[0].Quantity = 3;
        (await client.PostAsJsonAsync("/api/orders", request)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Waiter, 901));
        (await client.GetAsync($"/api/orders/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var foreign = new CreateOrderRequest { RequestId = Guid.NewGuid(), TableReference = "Mesa 1", Items = [new() { DishId = 90010, Quantity = 1 }] };
        (await client.PostAsJsonAsync("/api/orders", foreign)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Waiter, 900));
        (await client.PutAsJsonAsync($"/api/orders/{created.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Paid, 0))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var version = 0;
        foreach (var status in new[] { OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Completed, OrderStatus.Paid })
        {
            var role = status is OrderStatus.Preparing or OrderStatus.Ready ? IsmRoles.Chef : IsmRoles.Waiter;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(role, 900));
            var update = await client.PutAsJsonAsync($"/api/orders/{created.Id}/status", new UpdateOrderStatusRequest(status, version++));
            update.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = (await update.Content.ReadFromJsonAsync<OrderResponse>())!;
            updated.Status.Should().Be(status);
            if (status == OrderStatus.Paid) updated.PaidAtUtc.Should().NotBeNull();
        }
        (await client.PutAsJsonAsync($"/api/orders/{created.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Cancelled, 0))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Manager, 900));
        var metricsResponse = await client.GetAsync("/api/dashboard/metrics");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = (await metricsResponse.Content.ReadFromJsonAsync<DashboardResponse>())!;
        metrics.RevenueVsCost.Sum(point => point.Receita).Should().Be(80);
        metrics.RevenueVsCost.Sum(point => point.Custo).Should().Be(30);
        metrics.TopDishes.Should().ContainSingle().Which.Pedidos.Should().Be(2);
        (await client.GetAsync("/api/dashboard/metrics?restaurantId=901")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
