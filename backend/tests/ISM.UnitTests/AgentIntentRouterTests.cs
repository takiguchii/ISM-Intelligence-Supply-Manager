using System.Text.Json;
using FluentAssertions;
using ISM.Application.Services.Agents;

namespace ISM.UnitTests;

public sealed class AgentIntentRouterTests
{
    private readonly AgentIntentRouter _router = new();

    [Fact]
    public void ResolveStructured_AcceptsOnlyAllowlistedSchema()
    {
        using var document = JsonDocument.Parse("""
            {"specialist":"stock","toolName":"stock.getProduct","arguments":{"productId":42},"confidence":0.9}
            """);

        var intent = _router.ResolveStructured(document.RootElement);

        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be("stock.getProduct");
        intent.Confidence.Should().Be(0.9);
        intent.Input.GetProperty("productId").GetInt32().Should().Be(42);
    }

    [Theory]
    [InlineData("{\"specialist\":\"stock\",\"toolName\":\"unknown.tool\",\"arguments\":{}}")]
    [InlineData("{\"specialist\":\"stock\",\"toolName\":\"stock.getProduct\",\"arguments\":{\"productId\":42,\"restaurantId\":7}}")]
    [InlineData("{\"specialist\":\"finance\",\"toolName\":\"finance.getRevenueSummary\",\"arguments\":{\"extra\":true}}")]
    public void ResolveStructured_RejectsUnknownOrUnsafeSchema(string json)
    {
        using var document = JsonDocument.Parse(json);

        _router.ResolveStructured(document.RootElement).Should().BeNull();
    }

    [Fact]
    public void Resolve_ProvidesDeterministicSafeFallback()
    {
        var intent = _router.Resolve("consulte o saldo do produto 42");
        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be("stock.getProduct");
        intent.Input.GetProperty("productId").GetInt32().Should().Be(42);
        _router.Resolve("faça algo criativo").Should().BeNull();
    }

    [Theory]
    [InlineData("qual foi o último pedido?", "orders.getLatest")]
    [InlineData("qual foi o dia do último pedido feito?", "orders.getLatest")]
    [InlineData("como estão as vendas?", "orders.getRecent")]
    [InlineData("e os outros itens como estão?", "stock.getOverview")]
    [InlineData("como está a situação financeira do restaurante?", "finance.getRevenueSummary")]
    [InlineData("me mostre todos os pratos", "pricing.getDishOverview")]
    public void Resolve_Routes_practical_and_follow_up_questions(string question, string expectedTool)
    {
        var intent = _router.Resolve(question);
        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be(expectedTool);
    }

    [Theory]
    [InlineData("analise a reposição do produto 42", "stock.analyzeReplenishment")]
    [InlineData("quais itens estão abaixo do mínimo no estoque?", "stock.getOverview")]
    [InlineData("simule o preço sugerido para o prato 12", "pricing.simulateDishPrice")]
    [InlineData("compare fornecedores do arroz kg", "suppliers.compareOffers")]
    [InlineData("mostre quem vende arroz em kg", "suppliers.compareOffers")]
    [InlineData("qual fornecedor tem o menor preço do arroz kg", "suppliers.compareOffers")]
    [InlineData("resumo financeiro dos últimos 7 dias", "finance.getPeriodSummary")]
    [InlineData("calcule o consumo e os dias restantes do produto 42", "stock.analyzeReplenishment")]
    [InlineData("mostre a margem do prato 12", "pricing.getDishCost")]
    [InlineData("liste os pratos ativos do restaurante", "pricing.getDishOverview")]
    [InlineData("quais pratos estão cadastrados no cardápio?", "pricing.getDishOverview")]
    [InlineData("tem alguma cotação recente de arroz de carga kg?", "suppliers.compareOffers")]
    [InlineData("confira os preços recentes de arroz de carga kg", "suppliers.compareOffers")]
    [InlineData("quanto vendemos nos pedidos pagos recentes?", "orders.getRecent")]
    [InlineData("quais pratos mais venderam este mês?", "sales.getDishInsights")]
    [InlineData("previsão de vendas para a próxima semana", "sales.getDishInsights")]
    [InlineData("previsão de faturamento para a próxima semana", "finance.forecastRevenue")]
    [InlineData("o que mais nos deu lucro nessa última semana?", "sales.getDishInsights")]
    [InlineData("qual prato foi mais vendido e menos vendido na semana?", "sales.getDishInsights")]
    [InlineData("qual prato você acha que vai ter uma boa previsão com base nos nossos dados?", "sales.getDishInsights")]
    [InlineData("qual é o impacto do cardápio no estoque?", "stock.analyzeMenuImpact")]
    [InlineData("quais são os insumos mais consumidos pelas vendas?", "stock.analyzeMenuImpact")]
    public void Resolve_Selects_domain_tool_for_actionable_questions(string question, string expectedTool)
    {
        var intent = _router.Resolve(question);
        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be(expectedTool);
        if (question.Contains("lucro", StringComparison.OrdinalIgnoreCase))
            intent.Input.GetProperty("profit").GetBoolean().Should().BeTrue();
        if (question.Contains("menos vendido", StringComparison.OrdinalIgnoreCase))
            intent.Input.GetProperty("includeLeast").GetBoolean().Should().BeTrue();
        if (question.Contains("boa previsão", StringComparison.OrdinalIgnoreCase))
            intent.Input.GetProperty("forecast").GetBoolean().Should().BeTrue();
    }
}
