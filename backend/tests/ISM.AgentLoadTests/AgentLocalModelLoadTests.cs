using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ISM.Application.DTOs.Assistant;
using ISM.Application.Interfaces.Assistant;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Finance;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Orders;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Domain.Modules.Suppliers.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ISM.IntegrationTests;

[Trait("Category", "AgentLocalModelLoad")]
public sealed class AgentLocalModelLoadTests
{
    private const int RestaurantId = 876541;
    private const string JwtSecret = "agent-load-test-secret-key-with-32-bytes";
    private const string ProductName = "Arroz de Carga";
    private const string DishName = "Prato Executivo";
    private const string SupplierName = "Fornecedor Alpha";
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public AgentLocalModelLoadTests(Xunit.Abstractions.ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Each_agent_handles_twenty_real_tool_and_synthesis_prompts_with_qwen35_4b()
    {
        await using var factory = new AgentLoadWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var model = await scope.ServiceProvider.GetRequiredService<ILlmTextProvider>().GetStatusAsync();
        model.Reachable.Should().BeTrue($"Qwen local indisponível: {model.Error}");
        model.Model.Should().Be("qwen3.5:4b");

        var fixture = await SeedAsync(scope.ServiceProvider.GetRequiredService<IsmDbContext>());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());
        var results = new List<LoadResult>();
        var promptLimit = int.TryParse(Environment.GetEnvironmentVariable("ISM_AGENT_LOAD_PROMPT_LIMIT"), out var configuredLimit)
            ? Math.Clamp(configuredLimit, 1, 20)
            : 20;
        var requestedSpecialists = Environment.GetEnvironmentVariable("ISM_AGENT_LOAD_SPECIALISTS")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var requestedCases = Environment.GetEnvironmentVariable("ISM_AGENT_LOAD_CASES")?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value =>
            {
                var parts = value.Split(':', StringSplitOptions.TrimEntries);
                return parts.Length == 2 && int.TryParse(parts[1], out var caseIndex) && caseIndex > 0
                    ? new LoadCaseSelection(parts[0], caseIndex)
                    : throw new InvalidOperationException($"Caso de carga inválido: {value}");
            })
            .ToArray();
        var agentSets = AgentPrompts(fixture)
            .Where(agent => (requestedSpecialists is null || requestedSpecialists.Contains(agent.Specialist, StringComparer.OrdinalIgnoreCase))
                && (requestedCases is null || requestedCases.Any(item => item.Specialist.Equals(agent.Specialist, StringComparison.OrdinalIgnoreCase))))
            .ToArray();
        var expectedResultCount = 0;

        foreach (var agent in agentSets)
        {
            var create = await client.PostAsJsonAsync("/api/assistant/threads", new { specialistId = agent.Specialist });
            create.EnsureSuccessStatusCode();
            var conversation = await create.Content.ReadFromJsonAsync<AssistantConversationResponse>();
            conversation.Should().NotBeNull();

            var scenarios = requestedCases is not null
                ? agent.Prompts.Select((scenario, index) => (Scenario: scenario, Index: index + 1))
                    .Where(item => requestedCases.Any(selection => selection.Specialist.Equals(agent.Specialist, StringComparison.OrdinalIgnoreCase) && selection.Index == item.Index))
                    .ToArray()
                : (promptLimit == 20
                    ? agent.Prompts.Select((scenario, index) => (Scenario: scenario, Index: index + 1))
                    : Enumerable.Range(0, promptLimit).Select(index => (Scenario: agent.Prompts[index * agent.Prompts.Count / promptLimit], Index: index * agent.Prompts.Count / promptLimit + 1)))
                    .ToArray();
            expectedResultCount += scenarios.Length;
            foreach (var selected in scenarios)
            {
                var scenario = selected.Scenario;
                var index = selected.Index;
                var timer = Stopwatch.StartNew();
                var response = await client.PostAsJsonAsync($"/api/assistant/threads/{conversation!.Thread.Id}/messages", new { message = scenario.Prompt });
                timer.Stop();
                var updated = response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<AssistantConversationResponse>()
                    : null;
                var answer = updated?.Messages.LastOrDefault(message => message.Role == "assistant");
                var toolUsed = scenario.ExpectedTool is not null && scenario.ExpectedTool.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(expectedTool => answer?.Source?.Contains(expectedTool, StringComparison.OrdinalIgnoreCase) == true);
                var scopeDenied = answer?.Source == "Política de escopo do ISM";
                var factPresent = scenario.ExpectedFact is null || scenario.ExpectedFact.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .All(fact => answer?.Content.Contains(fact, StringComparison.OrdinalIgnoreCase) == true);
                var readable = answer is not null && answer.Content.Length is > 0 and <= 1800
                    && !answer.Content.Contains("\"ToolName\"", StringComparison.Ordinal)
                    && !answer.Content.Contains("\"restaurantId\"", StringComparison.Ordinal);
                var hasGroundedSynthesis = scenario.ExpectedTool is not null && answer?.Mode is "llm" or "deterministic";
                var passed = response.IsSuccessStatusCode && readable && factPresent
                    && (scenario.ExpectedDenied ? scopeDenied : toolUsed && hasGroundedSynthesis);
                var result = new LoadResult(agent.Specialist, index, scenario.Prompt, scenario.ExpectedTool,
                    scenario.ExpectedDenied, answer?.Source, answer?.Mode, answer?.Content, timer.ElapsedMilliseconds,
                    passed ? "PASS" : "FAIL", ClassifyFailure(scenario, answer, response.StatusCode, toolUsed, scopeDenied, factPresent, readable, hasGroundedSynthesis));
                results.Add(result);
                _output.WriteLine($"{result.Status} {agent.Specialist} #{index:00} {timer.ElapsedMilliseconds}ms {result.FailureClass}");
            }
        }

        var reportPath = WriteReport(results, model.Model);
        var summary = results.GroupBy(result => result.Specialist)
            .Select(group => $"{group.Key}: {group.Count(item => item.Status == "PASS")}/{group.Count()} PASS")
            .ToArray();
        _output.WriteLine($"Qwen={model.Model}; report={reportPath}; {string.Join(" | ", summary)}");
        results.Should().HaveCount(expectedResultCount);
        results.Where(result => result.ExpectedDenied).All(result => result.Status == "PASS").Should().BeTrue();
        results.Where(result => !result.ExpectedDenied).All(result => result.Status == "PASS").Should().BeTrue();
    }

    private static IReadOnlyList<AgentPromptSet> AgentPrompts(LoadFixture fixture)
    {
        static PromptCase Tool(string prompt, string tool, string? fact = null) => new(prompt, tool, false, fact);
        static PromptCase Deny(string prompt) => new(prompt, null, true, null);
        var p = fixture.ProductId;
        var d = fixture.DishId;
        return
        [
            new("stock", [
                Tool($"Consulte o saldo do produto {p}", "stock.getProduct", ProductName),
                Tool($"Quanto temos no estoque do produto {p}?", "stock.getProduct", ProductName),
                Tool($"Mostre os dados do produto {p} no estoque", "stock.getProduct", ProductName),
                Tool($"Confira o saldo atual do produto {p}", "stock.getProduct", ProductName),
                Tool($"Qual é o nível de estoque do produto {p}?", "stock.getProduct", ProductName),
                Tool($"Analise a reposição do produto {p}", "stock.analyzeReplenishment", ProductName),
                Tool($"Existe risco de ruptura para o produto {p}?", "stock.analyzeReplenishment", ProductName),
                Tool($"Calcule o consumo e os dias restantes do produto {p}", "stock.analyzeReplenishment", ProductName),
                Tool($"Devo repor o produto {p}?", "stock.analyzeReplenishment", ProductName),
                Tool($"Simule a necessidade de reposição do produto {p}", "stock.analyzeReplenishment", ProductName),
                Tool("Quais itens estão abaixo do mínimo no estoque?", "stock.getOverview", ProductName),
                Tool("Liste os itens críticos do estoque", "stock.getOverview", ProductName),
                Tool("Mostre os produtos abaixo do mínimo", "stock.getOverview", ProductName),
                Tool("Faça uma visão geral do estoque", "stock.getOverview", ProductName),
                Tool("Como está o estoque geral?", "stock.getOverview", ProductName),
                Deny("Qual foi o faturamento deste mês?"),
                Deny("Quais pratos têm o maior CMV?"),
                Deny("Compare os fornecedores do arroz kg"),
                Deny("Quanto pagamos de despesas este mês?"),
                Deny("Qual é a margem do Prato Executivo?")
            ]),
            new("pricing", [
                Tool($"Consulte o custo do prato {d}", "pricing.getDishCost", DishName),
                Tool($"Qual é o preço e o custo do prato {d}?", "pricing.getDishCost", DishName),
                Tool($"Mostre a margem do prato {d}", "pricing.getDishCost", DishName),
                Tool($"Confira o CMV do prato {d}", "pricing.getDishCost", DishName),
                Tool($"Detalhe a ficha técnica e o custo do prato {d}", "pricing.getDishCost", DishName),
                Tool($"Simule o preço sugerido para o prato {d}", "pricing.simulateDishPrice", DishName),
                Tool($"Faça uma simulação de preço para o prato {d}", "pricing.simulateDishPrice", DishName),
                Tool($"Como fica a margem se eu ajustar o preço do prato {d}?", "pricing.simulateDishPrice", DishName),
                Tool($"Calcule o preço para a margem alvo do prato {d}", "pricing.simulateDishPrice", DishName),
                Tool($"Simule o impacto no CMV do prato {d}", "pricing.simulateDishPrice", DishName),
                Tool("Mostre todos os pratos ativos", "pricing.getDishOverview", DishName),
                Tool("Faça um resumo dos pratos", "pricing.getDishOverview", DishName),
                Tool("Como está a situação dos pratos?", "pricing.getDishOverview", DishName),
                Tool("Liste os pratos e seus custos", "pricing.getDishOverview", DishName),
                Tool("Quais pratos estão cadastrados no cardápio?", "pricing.getDishOverview", DishName),
                Deny("Qual é o saldo do produto " + p + " no estoque?"),
                Deny("Compare fornecedores do arroz kg"),
                Deny("Qual foi a receita do restaurante este mês?"),
                Deny("Liste os itens com risco de ruptura"),
                Deny("Quanto temos de despesas a pagar?")
            ]),
            new("suppliers", [
                Tool("Compare fornecedores do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Compare ofertas do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Veja a cotação do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Quais fornecedores oferecem arroz de carga kg?", "suppliers.compareOffers", "Alpha"),
                Tool("Mostre as ofertas do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Compare o histórico de fornecedores do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Qual fornecedor tem o menor preço do arroz de carga kg?", "suppliers.compareOffers", "Alpha"),
                Tool("Consulte a cotação para arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Liste as ofertas para arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Quero comparar fornecedores para arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Mostre quem vende arroz de carga em kg", "suppliers.compareOffers", "Alpha"),
                Tool("Confira os preços recentes de arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Compare o preço de compra do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Tool("Tem alguma cotação recente de arroz de carga kg?", "suppliers.compareOffers", "Alpha"),
                Tool("Encontre a melhor oferta do arroz de carga kg", "suppliers.compareOffers", "Alpha"),
                Deny("Qual foi o faturamento deste mês?"),
                Deny("Mostre o saldo do produto " + p),
                Deny($"Simule o preço do prato {d}"),
                Deny("Quanto temos em contas a pagar?"),
                Deny("Quais itens estão abaixo do mínimo no estoque?")
            ]),
            new("finance", [
                Tool("Qual foi o faturamento deste mês?", "finance.getPeriodSummary", "70"),
                Tool("Mostre o resumo financeiro mensal", "finance.getRevenueSummary", "70"),
                Tool("Quanto entrou de receita neste mês?", "finance.getRevenueSummary|finance.getPeriodSummary", "70"),
                Tool("Quanto pagamos de despesas este mês?", "finance.getRevenueSummary|finance.getPeriodSummary", "10"),
                Tool("Como está a situação financeira do restaurante?", "finance.getRevenueSummary", "70"),
                Tool("Resuma receitas e despesas dos últimos 7 dias", "finance.getPeriodSummary", "70"),
                Tool("Qual foi nosso fluxo de caixa nos últimos 7 dias?", "finance.getPeriodSummary", "70"),
                Tool("Compare as entradas e despesas dos últimos 7 dias", "finance.getPeriodSummary", "70"),
                Tool("Quanto recebemos nos últimos 7 dias?", "finance.getPeriodSummary", "70"),
                Tool("Mostre o movimento financeiro dos últimos 7 dias", "finance.getPeriodSummary", "70"),
                Tool("Qual foi a receita do mês passado?", "finance.getPeriodSummary", "25"),
                Tool("Mostre o fluxo financeiro do mês passado", "finance.getPeriodSummary", "25"),
                Tool("Compare entradas e despesas do mês passado", "finance.getPeriodSummary", "25"),
                Tool("Quanto foi liquidado no mês passado?", "finance.getPeriodSummary", "25"),
                Tool("Resuma o mês passado no financeiro", "finance.getPeriodSummary", "25"),
                Tool("Quais foram as vendas recentes?", "orders.getRecent", "70"),
                Tool("Mostre os pedidos recentes e as vendas pagas", "orders.getRecent", "70"),
                Tool("Como estão as vendas?", "orders.getRecent", "70"),
                Tool("Quanto vendemos nos pedidos pagos recentes?", "orders.getRecent", "70"),
                Tool("Resuma o total dos pedidos recentes pagos", "orders.getRecent", "70"),
            ]),
            new("general", [
                Tool($"Resuma o faturamento e o estoque do produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Compare a situação financeira com o saldo do produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Como estão as receitas e o estoque do produto {p}?", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Analise o financeiro e consulte o produto {p} no estoque", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Quero ver faturamento e saldo do produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Traga receita mensal e situação do produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Mostre despesas e quantidade do produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Cruze o fluxo de caixa com o estoque do produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Avalie financeiro e estoque para o produto {p}", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Quais são as vendas e o saldo do produto {p}?", "finance.getRevenueSummary, stock.getProduct", ProductName),
                Tool($"Relacione faturamento e custo do prato {d}", "finance.getRevenueSummary, pricing.getDishCost", DishName),
                Tool($"Compare as receitas e o CMV do prato {d}", "finance.getRevenueSummary, pricing.getDishCost", DishName),
                Tool($"Mostre o resumo financeiro e o custo do prato {d}", "finance.getRevenueSummary, pricing.getDishCost", DishName),
                Tool($"Analise o faturamento junto com o preço do prato {d}", "finance.getRevenueSummary, pricing.getDishCost", $"{DishName}|R$ 70,00|R$ 35,00"),
                Tool($"Cruze o fluxo financeiro com o CMV do prato {d}", "finance.getRevenueSummary, pricing.getDishCost", DishName),
                Tool($"Consulte apenas o estoque do produto {p}", "stock.getProduct", ProductName),
                Tool("Mostre apenas o resumo financeiro do mês", "finance.getRevenueSummary", "70"),
                Tool($"Confira o custo somente do prato {d}", "pricing.getDishCost", DishName),
                Tool("Liste os pratos ativos do restaurante", "pricing.getDishOverview", DishName),
                Tool("Mostre os pedidos recentes pagos", "orders.getRecent", "70")
            ])
        ];
    }

    private static string ClassifyFailure(PromptCase scenario, AssistantMessageResponse? answer, System.Net.HttpStatusCode status,
        bool toolUsed, bool scopeDenied, bool factPresent, bool readable, bool usesLocalModel)
    {
        if ((int)status is < 200 or >= 300) return "http";
        if (!readable) return "formato";
        if (scenario.ExpectedDenied && !scopeDenied) return "isolamento";
        if (!scenario.ExpectedDenied && !toolUsed) return "roteamento/ferramenta";
        if (!scenario.ExpectedDenied && !factPresent) return "fato ausente";
        if (!scenario.ExpectedDenied && !usesLocalModel) return "síntese local/fallback";
        return answer?.Content.Contains("Dados insuficientes", StringComparison.OrdinalIgnoreCase) == true ? "dados" : string.Empty;
    }

    private static string WriteReport(IReadOnlyList<LoadResult> results, string model)
    {
        var root = FindRepositoryRoot();
        var path = Environment.GetEnvironmentVariable("ISM_AGENT_LOAD_REPORT_PATH")
            ?? Path.Combine(root, "docs", "planejamento", "relatorios", "P7-carga-qwen3.5-4b.json");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            model,
            generatedAtUtc = DateTime.UtcNow,
            total = results.Count,
            passed = results.Count(result => result.Status == "PASS"),
            failed = results.Count(result => result.Status == "FAIL"),
            bySpecialist = results.GroupBy(result => result.Specialist).Select(group => new
            {
                specialist = group.Key,
                total = group.Count(),
                passed = group.Count(result => result.Status == "PASS"),
                failed = group.Count(result => result.Status == "FAIL"),
                averageLatencyMs = (long)group.Average(result => result.LatencyMs),
                p95LatencyMs = group.OrderBy(result => result.LatencyMs).ElementAt((int)Math.Ceiling(group.Count() * .95) - 1).LatencyMs
            }),
            cases = results
        }, new JsonSerializerOptions { WriteIndented = true }));
        return Path.GetFullPath(path);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) return directory.FullName;
        throw new DirectoryNotFoundException("Não foi possível localizar a raiz do ISM para gravar o relatório de carga.");
    }

    private static string CreateToken()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(ClaimTypes.Role, IsmRoles.Manager),
            new Claim("restaurantId", RestaurantId.ToString())
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("ism-local-agent-load", "ism-local-agent-load", claims,
            expires: DateTime.UtcNow.AddHours(3), signingCredentials: credentials));
    }

    private static async Task<LoadFixture> SeedAsync(IsmDbContext db)
    {
        var now = DateTime.UtcNow;
        var restaurant = new Restaurant { Id = RestaurantId, Name = "Restaurante Sintético de Carga", CNPJ = "98765432100000", IsPlanActive = true, Created = now };
        db.Restaurants.Add(restaurant);
        var category = new Category { RestaurantId = RestaurantId, Name = "Carga", IsActive = true };
        db.Categories.Add(category);
        var product = new Product
        {
            RestaurantId = RestaurantId, Name = ProductName, Unit = "kg", CurrentQuantity = 8m,
            MinimumQuantity = 10m, MaximumQuantity = 40m, ReorderPoint = 12m, AverageCost = 5.5m, IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var dish = new Dish
        {
            RestaurantId = RestaurantId, CategoryId = category.Id, Name = DishName, Price = 35m, Cost = 11m,
            TargetMarginPercent = 60m, IsActive = true,
            Ingredients = [new DishIngredient { ProductId = product.Id, Product = product, Quantity = 2m }]
        };
        db.Dishes.Add(dish);
        db.StockMovements.Add(new StockMovement
        {
            RestaurantId = RestaurantId, ProductId = product.Id, MovementType = StockMovementType.ManualDecrease,
            QuantityDelta = -30m, CreatedAtUtc = now.AddDays(-1)
        });
        db.SupplierProductPriceHistories.AddRange(
            new SupplierProductPriceHistory { RestaurantId = RestaurantId, SupplierRawName = SupplierName, ProductRawName = "Arroz de Carga", Unit = "kg", UnitPrice = 5.5m, QuantityPurchased = 10m, PurchasedAtUtc = now.AddDays(-2) },
            new SupplierProductPriceHistory { RestaurantId = RestaurantId, SupplierRawName = "Fornecedor Beta", ProductRawName = "Arroz de Carga", Unit = "kg", UnitPrice = 6.25m, QuantityPurchased = 8m, PurchasedAtUtc = now.AddDays(-3) });
        db.FinanceEntries.AddRange(
            new FinanceEntry { RestaurantId = RestaurantId, Description = "Despesa atual", Category = "Insumos", Type = FinanceEntryType.Expense, Amount = 10m, DueDate = now.Date, SettledAtUtc = now.AddDays(-2) },
            new FinanceEntry { RestaurantId = RestaurantId, Description = "Despesa mês passado", Category = "Insumos", Type = FinanceEntryType.Expense, Amount = 25m, DueDate = now.AddDays(-40).Date, SettledAtUtc = now.AddDays(-40) });
        await db.SaveChangesAsync();

        var order = new Order
        {
            RestaurantId = RestaurantId, RequestId = Guid.NewGuid(), TableReference = "Carga-1", Status = OrderStatus.Paid,
            PaidAtUtc = now.AddDays(-1), CreatedAtUtc = now.AddDays(-1),
            Items = [new OrderItem { RestaurantId = RestaurantId, DishId = dish.Id, DishName = DishName, Quantity = 2, UnitPrice = 35m, UnitCost = 11m }]
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return new(product.Id, dish.Id);
    }

    private sealed class AgentLoadWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = "agent-load-" + Guid.NewGuid().ToString("N");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:SecretKey", JwtSecret);
            builder.UseSetting("Jwt:Issuer", "ism-local-agent-load");
            builder.UseSetting("Jwt:Audience", "ism-local-agent-load");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Jwt:SecretKey"] = JwtSecret,
                ["Jwt:Issuer"] = "ism-local-agent-load",
                ["Jwt:Audience"] = "ism-local-agent-load",
                ["AssistantAi:Enabled"] = "true",
                ["AssistantAi:OllamaBaseUrl"] = "http://127.0.0.1:11434",
                ["AssistantAi:Model"] = "qwen3.5:4b",
                ["AssistantAi:TimeoutSeconds"] = "240",
                ["AssistantAi:MaxOutputTokens"] = "180"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<IsmDbContext>>();
                services.AddDbContext<IsmDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }

    private sealed record LoadFixture(int ProductId, int DishId);
    private sealed record LoadCaseSelection(string Specialist, int Index);
    private sealed record PromptCase(string Prompt, string? ExpectedTool, bool ExpectedDenied, string? ExpectedFact);
    private sealed record AgentPromptSet(string Specialist, IReadOnlyList<PromptCase> Prompts);
    private sealed record LoadResult(string Specialist, int Index, string Prompt, string? ExpectedTool, bool ExpectedDenied,
        string? ActualSource, string? Mode, string? Response, long LatencyMs, string Status, string FailureClass);
}
