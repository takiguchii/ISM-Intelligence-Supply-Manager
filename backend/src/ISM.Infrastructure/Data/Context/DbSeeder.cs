using System.Security.Cryptography;
using System.Text;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Stock.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Data.Context;

public static class DbSeeder
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Pbkdf2Iterations = 100_000;
    private const byte FormatVersion = 0x01;
    public static async Task SeedAsync(IsmDbContext context)
    {
        // 0. Planos SaaS (Free / Pro / Enterprise) — sempre garante que existam
        if (!await context.Planos.AnyAsync())
        {
            var planos = new List<Plano>
            {
                new()
                {
                    Nome = "Free",
                    Descricao = "Plano trial básico para teste",
                    MaxUsuarios = 2,
                    MaxPratos = 10,
                    MaxProdutos = 20,
                    MaxCategorias = 3,
                    PrecoMensal = 0,
                    Ativo = true
                },
                new()
                {
                    Nome = "Pro",
                    Descricao = "Plano profissional para restaurantes em crescimento",
                    MaxUsuarios = 10,
                    MaxPratos = 100,
                    MaxProdutos = 200,
                    MaxCategorias = 15,
                    PrecoMensal = 149.90m,
                    Ativo = true
                },
                new()
                {
                    Nome = "Enterprise",
                    Descricao = "Plano ilimitado para redes e franquias",
                    MaxUsuarios = 100,
                    MaxPratos = 1000,
                    MaxProdutos = 2000,
                    MaxCategorias = 100,
                    PrecoMensal = 499.90m,
                    Ativo = true
                }
            };
            await context.Planos.AddRangeAsync(planos);
            await context.SaveChangesAsync();
        }

        var planoPro = await context.Planos.FirstOrDefaultAsync(p => p.Nome == "Pro");

        // 1. Seed usuário admin padrão (se não houver usuários)
        if (!await context.Users.AnyAsync())
        {
            var adminPassword = HashPassword("admin123");
            var admin = new User
            {
                Name = "Administrador ISM",
                Email = "admin@ism.com.br",
                PasswordHash = adminPassword,
                Role = "Admin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();
        }

        // Se já houver restaurantes cadastrados, não faz a seed de dados
        if (await context.Restaurants.AnyAsync())
        {
            return;
        }

        // 1. Cadastra Restaurante (vinculado ao Plano Pro)
        var restaurant = new Restaurant
        {
            Name = "Gourmet ISM Restaurant",
            CNPJ = "12345678000190",
            Created = DateTime.UtcNow,
            PlanoId = planoPro?.Id,
            TrialEndAtUtc = DateTime.UtcNow.AddDays(14),
            PlanoAtivo = true
        };

        await context.Restaurants.AddAsync(restaurant);
        await context.SaveChangesAsync(); // Salva para gerar o Id do Restaurante

        // 2. Cadastra Fornecedores (vinculados ao restaurante recém-criado)
        var fornecedores = new List<Fornecedor>
        {
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Distribuidora Alimentos Ltda",
                Category = "Grãos e Alimentos Secos",
                Description = "Distribuidora líder em grãos e alimentos não perecíveis com entrega em 24h.",
                Email = "vendas@distribuidoraalimentos.com",
                Phone = "(11) 99999-1111",
                IsActive = true
            },
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Hortifruti Central",
                Category = "Hortifrúti",
                Description = "Fornecedor de frutas, legumes e verduras frescas, colhidas diariamente.",
                Email = "pedidos@hortifruticentral.com.br",
                Phone = "(11) 98888-2222",
                IsActive = true
            }
        };

        await context.Fornecedores.AddRangeAsync(fornecedores);
        await context.SaveChangesAsync();

        // 3. Cadastra Produtos (Estoque) — agora com RestaurantId
        var products = new List<Product>
        {
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Arroz",
                Unit = "Kg",
                CurrentQuantity = 100m,
                MinimumQuantity = 20m,
                AverageCost = 5.50m,
                IsActive = true
            },
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Feijão",
                Unit = "Kg",
                CurrentQuantity = 80m,
                MinimumQuantity = 15m,
                AverageCost = 7.20m,
                IsActive = true
            },
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Carne Bovina",
                Unit = "Kg",
                CurrentQuantity = 50m,
                MinimumQuantity = 10m,
                AverageCost = 35.00m,
                IsActive = true
            },
            new()
            {
                RestaurantId = restaurant.Id,
                Name = "Tomate",
                Unit = "Kg",
                CurrentQuantity = 30m,
                MinimumQuantity = 5m,
                AverageCost = 4.50m,
                IsActive = true
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

    private static string HashPassword(string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        var bytes = new byte[1 + SaltSize + HashSize];
        bytes[0] = FormatVersion;
        Buffer.BlockCopy(salt, 0, bytes, 1, SaltSize);
        Buffer.BlockCopy(hash, 0, bytes, 1 + SaltSize, HashSize);

        return Convert.ToBase64String(bytes);
    }
}
