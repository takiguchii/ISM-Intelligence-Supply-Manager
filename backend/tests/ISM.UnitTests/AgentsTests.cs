using FluentAssertions;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Application.Services.Menu;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using Microsoft.Extensions.Options;
using Xunit;

namespace ISM.UnitTests;

public class AgentsTests
{
    private class FakeDishRepository : IDishRepository
    {
        public List<Dish> Dishes { get; set; } = new();

        public Task<Dish?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(Dishes.FirstOrDefault(d => d.Id == id));

        public Task<IReadOnlyCollection<Dish>> GetAllDishesAsync(int? restaurantId = null, int? categoryId = null, CancellationToken cancellationToken = default)
        {
            var result = Dishes.AsEnumerable();
            if (restaurantId.HasValue) result = result.Where(d => d.RestaurantId == restaurantId.Value);
            if (categoryId.HasValue) result = result.Where(d => d.CategoryId == categoryId.Value);
            return Task.FromResult<IReadOnlyCollection<Dish>>(result.ToList());
        }

        public Task<Dish> AddDishAsync(Dish dish, CancellationToken cancellationToken = default)
        {
            Dishes.Add(dish);
            return Task.FromResult(dish);
        }

        public Task<Dish?> UpdateDishAsync(int id, Dish dish, IReadOnlyCollection<DishIngredient> ingredients, CancellationToken cancellationToken = default)
        {
            var existing = Dishes.FirstOrDefault(d => d.Id == id);
            if (existing != null)
            {
                existing.Cost = dish.Cost;
                existing.Price = dish.Price;
            }
            return Task.FromResult(existing);
        }

        public Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IReadOnlyCollection<Dish>> GetDishesByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var result = Dishes.Where(d => d.Ingredients.Any(i => i.ProductId == productId)).ToList();
            return Task.FromResult<IReadOnlyCollection<Dish>>(result);
        }
    }

    private class FakeSystemAlertRepository : ISystemAlertRepository
    {
        public List<SystemAlert> Alerts { get; } = new();

        public Task<SystemAlert> CreateAsync(SystemAlert alert, CancellationToken cancellationToken = default)
        {
            alert.Id = Alerts.Count + 1;
            Alerts.Add(alert);
            return Task.FromResult(alert);
        }

        public Task<SystemAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(Alerts.FirstOrDefault(a => a.Id == id));

        public Task<(IReadOnlyList<SystemAlert> Items, int TotalCount)> GetPagedAsync(
            int? restaurantId, int pageNumber, int pageSize, SystemAlertType? alertType = null,
            AlertSeverity? severity = null, bool? isRead = null, bool? isDismissed = null,
            string? search = null, CancellationToken cancellationToken = default)
        {
            var query = Alerts.AsEnumerable();
            if (restaurantId.HasValue) query = query.Where(a => a.RestaurantId == restaurantId.Value);
            var list = query.ToList();
            return Task.FromResult<(IReadOnlyList<SystemAlert>, int)>((list, list.Count));
        }

        public Task<bool> ExistsSameTypeNonDismissedWithinAsync(
            int restaurantId, SystemAlertType alertType, string? referenceEntityType,
            int? referenceEntityId, TimeSpan withinWindow, string? deduplicationKey = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<SystemAlert?> UpdateAsync(SystemAlert alert, CancellationToken cancellationToken = default)
            => Task.FromResult<SystemAlert?>(alert);
    }

    private class FakeRestaurantRepository : IRestaurantRepository
    {
        public Task<Restaurant?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Restaurant?>(new Restaurant { Id = id, Name = "Restaurante Teste" });

        public Task<IReadOnlyList<Restaurant>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Restaurant>>(new List<Restaurant> { new() { Id = 1, Name = "Restaurante Teste" } });

        public Task AddRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Restaurant restaurant) { }
        public void Delete(Restaurant restaurant) { }
        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private class FakeCurrentUser : ICurrentUser
    {
        public int UserId => 1;
        public int? RestaurantId => 1;
        public string Email => "admin@ism.com";
        public string Role => "Admin";
        public bool IsSuperAdmin => false;
        public bool IsManagerOrAbove => true;
    }

    [Fact]
    public async Task PricingAgent_Should_GenerateAlert_When_IngredientCostIncreasesAndBreachesMargin()
    {
        // Arrange: Insumo "Carne Mignon" que subiu de preço
        var carneProduct = new Product
        {
            Id = 10,
            Name = "Carne Mignon",
            Unit = "KG",
            AverageCost = 60.00m // Preço subiu de 40.00m para 60.00m
        };

        var pratoFile = new Dish
        {
            Id = 1,
            RestaurantId = 1,
            Name = "Filé Mignon ao Poivre",
            Price = 80.00m, // Preço de venda
            Cost = 20.00m,  // Custo antigo
            TargetMarginPercent = 60.00m, // Margem alvo esperada: 60%
            Ingredients = new List<DishIngredient>
            {
                new() { ProductId = 10, Product = carneProduct, Quantity = 0.8m } // 0.8kg * R$60 = R$48 custo
            }
        };

        var dishRepo = new FakeDishRepository();
        dishRepo.Dishes.Add(pratoFile);

        var alertRepo = new FakeSystemAlertRepository();
        var restaurantRepo = new FakeRestaurantRepository();
        var currentUser = new FakeCurrentUser();
        var options = Options.Create(new PricingOptions { DefaultTargetMarginPercent = 60.00m });

        var pricingService = new PricingCmvService(dishRepo, restaurantRepo, alertRepo, currentUser, options);

        // Act: Executa o recálculo do agente de precificação para o produto
        var alerts = await pricingService.RecalculateForProductAsync(10);

        // Assert: Verifica se o novo custo foi recalculado (0.8 * 60 = 48), CMV atual = (48/80)*100 = 60% (margem real 40% < 60% alvo)
        pratoFile.Cost.Should().Be(48.00m);
        alerts.Should().HaveCount(1);
        alerts[0].DishId.Should().Be(1);
        alerts[0].CurrentCmvPercent.Should().Be(60.00m);
        alerts[0].SuggestedPrice.Should().Be(120.00m); // Custo R$48 / (1 - 0.6) = R$120

        // Verifica se o alerta foi registrado na tabela de SystemAlert do banco
        alertRepo.Alerts.Should().HaveCount(1);
        alertRepo.Alerts[0].AlertType.Should().Be(SystemAlertType.DishCmvDefasagem);
        alertRepo.Alerts[0].ReferenceEntityId.Should().Be(1);
        alertRepo.Alerts[0].Title.Should().Contain("Defasagem de CMV: Filé Mignon ao Poivre");
    }

    [Fact]
    public async Task PricingAgent_RunAsync_Should_Process_All_Dishes_In_Background()
    {
        // Arrange
        var produtoQueijo = new Product { Id = 20, Name = "Queijo Mozarela", Unit = "KG", AverageCost = 50.00m };
        var pratoPizza = new Dish
        {
            Id = 2,
            RestaurantId = 1,
            Name = "Pizza Mozarela",
            Price = 50.00m,
            Cost = 15.00m,
            TargetMarginPercent = 65.00m,
            Ingredients = new List<DishIngredient> { new() { ProductId = 20, Product = produtoQueijo, Quantity = 0.5m } } // 0.5 * 50 = R$25 custo. CMV (25/50)*100 = 50% > (100 - 65)%
        };

        var dishRepo = new FakeDishRepository();
        dishRepo.Dishes.Add(pratoPizza);

        var alertRepo = new FakeSystemAlertRepository();
        var restaurantRepo = new FakeRestaurantRepository();
        var currentUser = new FakeCurrentUser();
        var options = Options.Create(new PricingOptions { DefaultTargetMarginPercent = 65.00m });

        var pricingService = new PricingCmvService(dishRepo, restaurantRepo, alertRepo, currentUser, options);

        // Act: Executa o ciclo periódico do agente em background
        var summary = await pricingService.RunAsync();

        // Assert
        summary.RestaurantsProcessed.Should().Be(1);
        summary.EntitiesEvaluated.Should().Be(1);
        summary.AlertsCreated.Should().Be(1);
        alertRepo.Alerts.Should().HaveCount(1);
        alertRepo.Alerts[0].AlertType.Should().Be(SystemAlertType.DishCmvDefasagem);
    }
}
