using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.IntegrationTests;

public sealed class ProductStockLifecycleTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductStockLifecycleTests(FoundationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Product_CreateUpdateDelete_FullLifecycle_Succeeds()
    {
        const int tenantId = 888;

        // Garante que o restaurante e o plano existam no banco para verificação de limites
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
            var plan = await db.Plans.FindAsync(2);
            if (plan is null)
            {
                plan = new Plan
                {
                    Id = 2,
                    Name = "Pro",
                    Description = "Plano Pro",
                    MaxUsers = 10,
                    MaxDishes = 100,
                    MaxProducts = 200,
                    MaxCategories = 20,
                    MonthlyPrice = 149.90m,
                    IsActive = true
                };
                db.Plans.Add(plan);
            }

            var restaurant = await db.Restaurants.FindAsync(tenantId);
            if (restaurant is null)
            {
                db.Restaurants.Add(new Restaurant
                {
                    Id = tenantId,
                    Name = "Restaurante Teste Lifecycle",
                    CNPJ = "11222333000199",
                    PlanId = plan.Id,
                    IsPlanActive = true,
                    Created = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            FoundationWebApplicationFactory.Token(IsmRoles.Manager, tenantId)
        );

        // 1. Cadastrar produto
        var createRequest = new CreateProductRequest(
            RestaurantId: tenantId,
            Name: "Picanha Nobre Bovina",
            Unit: "kg",
            CurrentQuantity: 15.5m,
            MinimumQuantity: 10.0m,
            MaximumQuantity: 50.0m,
            ReorderPoint: 12.0m,
            AverageCost: 79.90m,
            MovingAverageConsumption: 2.5m
        );

        var postResponse = await _client.PostAsJsonAsync("/api/stock/products", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await postResponse.Content.ReadFromJsonAsync<ProductResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Picanha Nobre Bovina");
        created.CurrentQuantity.Should().Be(15.5m);
        created.AverageCost.Should().Be(79.90m);

        // 2. Consultar produto por ID
        var getResponse = await _client.GetAsync($"/api/stock/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Picanha Nobre Bovina");

        // 3. Consultar na lista paginada
        var pagedResponse = await _client.GetAsync("/api/stock/products/paged?search=Picanha");
        pagedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await pagedResponse.Content.ReadFromJsonAsync<PagedResult<ProductResponse>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().Contain(p => p.Id == created.Id && p.Name == "Picanha Nobre Bovina");

        // 4. Editar produto
        var updateRequest = new UpdateProductRequest(
            Name: "Picanha Nobre Bovina Especial",
            Unit: "kg",
            CurrentQuantity: 25.0m,
            MinimumQuantity: 12.0m,
            MaximumQuantity: 60.0m,
            ReorderPoint: 15.0m,
            AverageCost: 82.50m,
            MovingAverageConsumption: 3.0m
        );

        var putResponse = await _client.PutAsJsonAsync($"/api/stock/products/{created.Id}", updateRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Picanha Nobre Bovina Especial");
        updated.CurrentQuantity.Should().Be(25.0m);
        updated.AverageCost.Should().Be(82.50m);

        // 5. Excluir produto
        var deleteResponse = await _client.DeleteAsync($"/api/stock/products/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. Verificar que foi desativado/removido
        var getAfterDelete = await _client.GetAsync($"/api/stock/products/{created.Id}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
