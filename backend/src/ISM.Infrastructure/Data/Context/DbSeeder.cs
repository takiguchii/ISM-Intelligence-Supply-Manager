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
        // 0. Planos SaaS (Free / Pro / Enterprise)
        if (!await context.Plans.AnyAsync())
        {
            var plans = new List<Plan>
            {
                new()
                {
                    Name = "Free",
                    Description = "Plano trial básico para avaliação",
                    MaxUsers = 2,
                    MaxDishes = 10,
                    MaxProducts = 20,
                    MaxCategories = 3,
                    MonthlyPrice = 0,
                    IsActive = true
                },
                new()
                {
                    Name = "Pro",
                    Description = "Plano profissional para restaurantes em crescimento",
                    MaxUsers = 10,
                    MaxDishes = 100,
                    MaxProducts = 200,
                    MaxCategories = 15,
                    MonthlyPrice = 149.90m,
                    IsActive = true
                },
                new()
                {
                    Name = "Enterprise",
                    Description = "Plano ilimitado para redes e franquias",
                    MaxUsers = 100,
                    MaxDishes = 1000,
                    MaxProducts = 2000,
                    MaxCategories = 100,
                    MonthlyPrice = 499.90m,
                    IsActive = true
                }
            };
            await context.Plans.AddRangeAsync(plans);
            await context.SaveChangesAsync();
        }

        var planPro = await context.Plans.FirstOrDefaultAsync(p => p.Name == "Pro");

        // 1. Cadastra Restaurante padrão (se não existir)
        Restaurant? restaurant = await context.Restaurants.FirstOrDefaultAsync();
        if (restaurant is null)
        {
            restaurant = new Restaurant
            {
                Name = "Gourmet ISM Restaurante & Bar",
                CNPJ = "12345678000190",
                Created = DateTime.UtcNow,
                PlanId = planPro?.Id,
                TrialEndAtUtc = DateTime.UtcNow.AddDays(30),
                IsPlanActive = true
            };

            await context.Restaurants.AddAsync(restaurant);
            await context.SaveChangesAsync();
        }

        // 2. Usuários padrão por cargo (Admin, Manager, Chef, Waiter)
        if (!await context.Users.AnyAsync())
        {
            var defaultPassword = HashPassword("admin123");
            var users = new List<User>
            {
                new()
                {
                    Name = "Super Administrador ISM",
                    Email = "admin@ism.com.br",
                    PasswordHash = defaultPassword,
                    Role = "Admin",
                    RestaurantId = restaurant.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Name = "Mariana Gerente",
                    Email = "gerente@ism.com.br",
                    PasswordHash = defaultPassword,
                    Role = "Manager",
                    RestaurantId = restaurant.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Name = "Chef Carlos Silva",
                    Email = "chef@ism.com.br",
                    PasswordHash = defaultPassword,
                    Role = "Chef",
                    RestaurantId = restaurant.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Name = "Lucas Garçom",
                    Email = "garcom@ism.com.br",
                    PasswordHash = defaultPassword,
                    Role = "Waiter",
                    RestaurantId = restaurant.Id,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        // 3. Fornecedores diversificados
        if (!await context.Suppliers.AnyAsync())
        {
            var suppliers = new List<Supplier>
            {
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Distribuidora Grãos Brasil",
                    Category = "Grãos e Alimentos Secos",
                    Description = "Líder em distribuição de arroz, feijão, farinhas e grãos selecionados.",
                    Email = "pedidos@graosbrasil.com.br",
                    Phone = "(11) 99111-1001",
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Hortifruti da Serra",
                    Category = "Hortifrúti",
                    Description = "Verduras, legumes e frutas frescas com colheita e entrega diária.",
                    Email = "contato@hortifrutidaserra.com.br",
                    Phone = "(11) 99222-2002",
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Frigorífico Boi Nobre",
                    Category = "Carnes",
                    Description = "Cortes nobres bovinos, suínos e aves com certificado de qualidade.",
                    Email = "comercial@boinobre.com.br",
                    Phone = "(11) 99333-3003",
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Mar Fresco Pescados",
                    Category = "Frutos do Mar",
                    Description = "Peixes nobres, salmão importado, camarões e frutos do mar frescos.",
                    Email = "vendas@marfrescopescados.com.br",
                    Phone = "(13) 99444-4004",
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Laticínios Vale Real",
                    Category = "Laticínios",
                    Description = "Queijos artesanais, mussarela fatiada, leite, manteiga e derivados.",
                    Email = "sac@laticiniosvalereal.com.br",
                    Phone = "(11) 99555-5005",
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Bebidas Premium Paulista",
                    Category = "Bebidas",
                    Description = "Sucos naturais, refrigerantes, água mineral, vinhos e cervejas artesanais.",
                    Email = "atendimento@bebidaspremium.com.br",
                    Phone = "(11) 99666-6006",
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "EcoPack Embalagens",
                    Category = "Embalagens",
                    Description = "Embalagens sustentáveis e biodegradáveis para delivery e salão.",
                    Email = "pedidos@ecopack.com.br",
                    Phone = "(11) 99777-7007",
                    IsActive = true
                }
            };

            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }

        // 4. Insumos do Estoque (Mix saudável + insumos em estado crítico para acionar IA)
        if (!await context.Products.AnyAsync())
        {
            var products = new List<Product>
            {
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Arroz Agulhinha Tipo 1",
                    Unit = "kg",
                    CurrentQuantity = 120m,
                    MinimumQuantity = 30m,
                    AverageCost = 5.80m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Feijão Carioca Especial",
                    Unit = "kg",
                    CurrentQuantity = 85m,
                    MinimumQuantity = 25m,
                    AverageCost = 7.50m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Filé Mignon Bovino",
                    Unit = "kg",
                    CurrentQuantity = 12m,
                    MinimumQuantity = 20m, // Crítico!
                    AverageCost = 64.00m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Peito de Frango Resfriado",
                    Unit = "kg",
                    CurrentQuantity = 50m,
                    MinimumQuantity = 15m,
                    AverageCost = 18.50m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Salmão Fresco em Filé",
                    Unit = "kg",
                    CurrentQuantity = 6m,
                    MinimumQuantity = 15m, // Crítico!
                    AverageCost = 88.00m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Queijo Mussarela Fatiado",
                    Unit = "kg",
                    CurrentQuantity = 35m,
                    MinimumQuantity = 10m,
                    AverageCost = 38.00m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Tomate Italiano Fresco",
                    Unit = "kg",
                    CurrentQuantity = 30m,
                    MinimumQuantity = 8m,
                    AverageCost = 6.20m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Cebola Roxa",
                    Unit = "kg",
                    CurrentQuantity = 22m,
                    MinimumQuantity = 5m,
                    AverageCost = 4.90m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Azeite de Oliva Extra Virgem",
                    Unit = "L",
                    CurrentQuantity = 16m,
                    MinimumQuantity = 5m,
                    AverageCost = 42.00m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Leite Integral Pasteurizado",
                    Unit = "L",
                    CurrentQuantity = 45m,
                    MinimumQuantity = 12m,
                    AverageCost = 4.70m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Café Gourmet em Grãos",
                    Unit = "kg",
                    CurrentQuantity = 14m,
                    MinimumQuantity = 4m,
                    AverageCost = 52.00m,
                    IsActive = true
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Laranja Pera Fresca",
                    Unit = "kg",
                    CurrentQuantity = 60m,
                    MinimumQuantity = 15m,
                    AverageCost = 3.80m,
                    IsActive = true
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        // 5. Categorias do Cardápio
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Entradas & Petiscos",
                    IsActive = true,
                    DisplayOrder = 1
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Pratos Principais",
                    IsActive = true,
                    DisplayOrder = 2
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Sobremesas",
                    IsActive = true,
                    DisplayOrder = 3
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    Name = "Bebidas & Cafeteria",
                    IsActive = true,
                    DisplayOrder = 4
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 6. Pratos e Fichas Técnicas
        if (!await context.Dishes.AnyAsync())
        {
            var allCategories = await context.Categories.Where(c => c.RestaurantId == restaurant.Id).ToListAsync();
            var allProducts = await context.Products.Where(p => p.RestaurantId == restaurant.Id).ToListAsync();

            var catEntrada = allCategories.FirstOrDefault(c => c.Name.Contains("Entradas")) ?? allCategories[0];
            var catPrincipal = allCategories.FirstOrDefault(c => c.Name.Contains("Principais")) ?? allCategories[0];
            var catSobremesa = allCategories.FirstOrDefault(c => c.Name.Contains("Sobremesas")) ?? allCategories[0];
            var catBebidas = allCategories.FirstOrDefault(c => c.Name.Contains("Bebidas")) ?? allCategories[0];

            var pArroz = allProducts.FirstOrDefault(p => p.Name.Contains("Arroz"));
            var pFeijao = allProducts.FirstOrDefault(p => p.Name.Contains("Feijão"));
            var pMignon = allProducts.FirstOrDefault(p => p.Name.Contains("Mignon"));
            var pFrango = allProducts.FirstOrDefault(p => p.Name.Contains("Frango"));
            var pSalmao = allProducts.FirstOrDefault(p => p.Name.Contains("Salmão"));
            var pQueijo = allProducts.FirstOrDefault(p => p.Name.Contains("Mussarela"));
            var pTomate = allProducts.FirstOrDefault(p => p.Name.Contains("Tomate"));
            var pAzeite = allProducts.FirstOrDefault(p => p.Name.Contains("Azeite"));
            var pCafe = allProducts.FirstOrDefault(p => p.Name.Contains("Café"));
            var pLaranja = allProducts.FirstOrDefault(p => p.Name.Contains("Laranja"));

            var pratos = new List<Dish>
            {
                new()
                {
                    RestaurantId = restaurant.Id,
                    CategoryId = catPrincipal.Id,
                    Name = "Filé Mignon ao Molho Madeira",
                    Description = "Medalhão de filé mignon grelhado ao molho madeira, servido com arroz branco e batatas salteadas no azeite.",
                    Price = 78.90m,
                    Cost = 28.50m,
                    IsActive = true,
                    Highlight = true,
                    DisplayOrder = 1,
                    UrlImage = "https://images.unsplash.com/photo-1544025162-d76694265947"
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    CategoryId = catPrincipal.Id,
                    Name = "Salmão Nobre Grelhado",
                    Description = "Posta de salmão fresco grelhado com crosta de ervas finas e legumes salteados.",
                    Price = 89.00m,
                    Cost = 35.00m,
                    IsActive = true,
                    Highlight = true,
                    DisplayOrder = 2,
                    UrlImage = "https://images.unsplash.com/photo-1467003909585-2f8a72700288"
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    CategoryId = catPrincipal.Id,
                    Name = "Prato Executivo Tradicional ISM",
                    Description = "Arroz branco soltinho, feijão carioca temperado, filé de frango grelhado e vinagrete de tomate fresco.",
                    Price = 34.90m,
                    Cost = 11.80m,
                    IsActive = true,
                    Highlight = false,
                    DisplayOrder = 3,
                    UrlImage = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    CategoryId = catEntrada.Id,
                    Name = "Bruschetta Italiana Rústica",
                    Description = "Pão italiano tostado com cobertura de tomates frescos marinados no azeite de oliva e queijo mussarela gratinado.",
                    Price = 28.00m,
                    Cost = 8.50m,
                    IsActive = true,
                    Highlight = true,
                    DisplayOrder = 1,
                    UrlImage = "https://images.unsplash.com/photo-1572695157366-5e585ab2b69f"
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    CategoryId = catBebidas.Id,
                    Name = "Suco Natural de Laranja 500ml",
                    Description = "Suco fresco 100% natural espremido na hora com laranjas selecionadas.",
                    Price = 12.00m,
                    Cost = 3.20m,
                    IsActive = true,
                    Highlight = false,
                    DisplayOrder = 1,
                    UrlImage = "https://images.unsplash.com/photo-1600271886742-f049cd451bba"
                },
                new()
                {
                    RestaurantId = restaurant.Id,
                    CategoryId = catBebidas.Id,
                    Name = "Café Espresso Duplo Especial",
                    Description = "Café espresso encorpado extraído com grãos 100% arábica gourmet.",
                    Price = 8.50m,
                    Cost = 1.80m,
                    IsActive = true,
                    Highlight = false,
                    DisplayOrder = 2,
                    UrlImage = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd"
                }
            };

            await context.Dishes.AddRangeAsync(pratos);
            await context.SaveChangesAsync();

            // Ficha técnica (DishIngredients)
            var ingredients = new List<DishIngredient>();

            if (pMignon is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[0].Id, ProductId = pMignon.Id, Quantity = 0.25m });
            if (pArroz is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[0].Id, ProductId = pArroz.Id, Quantity = 0.15m });
            if (pAzeite is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[0].Id, ProductId = pAzeite.Id, Quantity = 0.03m });

            if (pSalmao is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[1].Id, ProductId = pSalmao.Id, Quantity = 0.25m });
            if (pAzeite is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[1].Id, ProductId = pAzeite.Id, Quantity = 0.04m });

            if (pArroz is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[2].Id, ProductId = pArroz.Id, Quantity = 0.20m });
            if (pFeijao is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[2].Id, ProductId = pFeijao.Id, Quantity = 0.15m });
            if (pFrango is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[2].Id, ProductId = pFrango.Id, Quantity = 0.20m });
            if (pTomate is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[2].Id, ProductId = pTomate.Id, Quantity = 0.08m });

            if (pTomate is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[3].Id, ProductId = pTomate.Id, Quantity = 0.15m });
            if (pQueijo is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[3].Id, ProductId = pQueijo.Id, Quantity = 0.08m });
            if (pAzeite is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[3].Id, ProductId = pAzeite.Id, Quantity = 0.03m });

            if (pLaranja is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[4].Id, ProductId = pLaranja.Id, Quantity = 0.50m });

            if (pCafe is not null)
                ingredients.Add(new DishIngredient { DishId = pratos[5].Id, ProductId = pCafe.Id, Quantity = 0.02m });

            if (ingredients.Count > 0)
            {
                await context.DishIngredients.AddRangeAsync(ingredients);
                await context.SaveChangesAsync();
            }
        }
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
