using ISM.Domain.Entities;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Stock.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Data.Context;

public static class DbSeeder
{
    public static async Task SeedAsync(IsmDbContext context)
    {
        // Se já houver restaurantes ou fornecedores cadastrados, não faz a seed
        if (await context.Restaurants.AnyAsync() || await context.Fornecedores.AnyAsync())
        {
            return;
        }

        // 1. Cadastra Fornecedores
        var fornecedores = new List<Fornecedor>
        {
            new()
            {
                Nome = "Distribuidora Alimentos Ltda",
                Categoria = "Grãos e Alimentos Secos",
                Email = "vendas@distribuidoraalimentos.com",
                Telefone = "(11) 99999-1111"
            },
            new()
            {
                Nome = "Hortifruti Central",
                Categoria = "Hortifrúti",
                Email = "pedidos@hortifruticentral.com.br",
                Telefone = "(11) 98888-2222"
            }
        };

        await context.Fornecedores.AddRangeAsync(fornecedores);

        // 2. Cadastra Restaurante
        var restaurant = new Restaurant
        {
            Name = "Gourmet ISM Restaurant",
            CNPJ = "12345678000190",
            Created = DateTime.UtcNow
        };

        await context.Restaurants.AddAsync(restaurant);
        await context.SaveChangesAsync(); // Salva para gerar o Id do Restaurante

        // 3. Cadastra Produtos (Estoque)
        var products = new List<Product>
        {
            new()
            {
                Name = "Arroz",
                Unit = "Kg",
                CurrentQuantity = 100m,
                MinimumQuantity = 20m,
                AverageCost = 5.50m
            },
            new()
            {
                Name = "Feijão",
                Unit = "Kg",
                CurrentQuantity = 80m,
                MinimumQuantity = 15m,
                AverageCost = 7.20m
            },
            new()
            {
                Name = "Carne Bovina",
                Unit = "Kg",
                CurrentQuantity = 50m,
                MinimumQuantity = 10m,
                AverageCost = 35.00m
            },
            new()
            {
                Name = "Tomate",
                Unit = "Kg",
                CurrentQuantity = 30m,
                MinimumQuantity = 5m,
                AverageCost = 4.50m
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync(); // Salva para gerar os Ids dos produtos

        // 4. Cadastra Categorias do Menu
        var categories = new List<Category>
        {
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Pratos Principais",
                IsActive = true,
                DisplayOrder = 1
            },
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Bebidas",
                IsActive = true,
                DisplayOrder = 2
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync(); // Salva para gerar os Ids das categorias

        // 5. Cadastra Pratos (Dishes)
        var pratoFeito = new Dish
        {
            RestaurantId = restaurant.Id,
            CategoryId = categories[0].Id,
            Name = "Prato Feito Tradicional",
            Description = "Arroz soltinho, feijão temperado e filé de carne bovina grelhada",
            Price = 25.90m,
            Cost = 12.00m,
            IsActive = true,
            Highlight = true,
            DisplayOrder = 1,
            UrlImage = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"
        };

        var sucoTomate = new Dish
        {
            RestaurantId = restaurant.Id,
            CategoryId = categories[1].Id,
            Name = "Suco Natural de Tomate",
            Description = "Suco refrescante preparado com tomates frescos selecionados",
            Price = 8.90m,
            Cost = 2.50m,
            IsActive = true,
            Highlight = false,
            DisplayOrder = 2,
            UrlImage = "https://images.unsplash.com/photo-1600271886742-f049cd451bba"
        };

        await context.Dishes.AddRangeAsync(pratoFeito, sucoTomate);
        await context.SaveChangesAsync(); // Salva para gerar os Ids dos pratos

        // 6. Cadastra os Ingredientes dos Pratos (DishIngredients)
        var ingredients = new List<DishIngredient>
        {
            new()
            {
                DishId = pratoFeito.Id,
                ProductId = products[0].Id, // Arroz
                Quantity = 0.20m
            },
            new()
            {
                DishId = pratoFeito.Id,
                ProductId = products[1].Id, // Feijão
                Quantity = 0.15m
            },
            new()
            {
                DishId = pratoFeito.Id,
                ProductId = products[2].Id, // Carne Bovina
                Quantity = 0.20m
            },
            new()
            {
                DishId = sucoTomate.Id,
                ProductId = products[3].Id, // Tomate
                Quantity = 0.30m
            }
        };

        await context.DishIngredients.AddRangeAsync(ingredients);
        await context.SaveChangesAsync();
    }
}
