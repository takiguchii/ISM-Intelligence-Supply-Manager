using FluentAssertions;
using ISM.Application.DTOs.Assistant;
using ISM.Application.Services.Agents;
using ISM.Application.Services.Assistant;

namespace ISM.UnitTests;

public sealed class AssistantIntentCoordinatorTests
{
    private readonly AssistantIntentCoordinator _coordinator = new(new AgentIntentRouter());

    public static IEnumerable<object[]> Scenarios()
    {
        yield return ["faturamento do mês", null!, "finance.getRevenueSummary"];
        yield return ["qual a receita?", null!, "finance.getRevenueSummary"];
        yield return ["repor este produto", ProductContext(), "stock.analyzeReplenishment"];
        yield return ["devo comprar agora?", ProductContext(), "stock.analyzeReplenishment"];
        yield return ["simule o preço", DishContext(), "pricing.simulateDishPrice"];
        yield return ["qual o CMV?", DishContext(), "pricing.simulateDishPrice"];
        yield return ["compare fornecedores", ProductContext(), "suppliers.compareOffers"];
        yield return ["mostre os detalhes do alerta", ProductContext(), "assistant.getAlertContext"];
        yield return ["consulte o saldo do produto 42", null!, "stock.getProduct"];
        yield return ["consulte o custo do prato 9", null!, "pricing.getDishCost"];
        yield return ["pergunte algo sem domínio", null!, null!];
        yield return ["faça uma mágica", null!, null!];
        yield return ["qual o clima hoje?", null!, null!];
        yield return ["repor produto", DishContext(), null!];
        yield return ["simule preço", ProductContext(), null!];
        yield return ["compare fornecedores", DishContext(), null!];
        yield return ["histórico", null!, null!];
        yield return ["detalhe", null!, null!];
        yield return ["comprar", null!, null!];
        yield return ["margem", null!, null!];
        yield return ["faturamento de outro restaurante", null!, "finance.getRevenueSummary"];
        yield return ["produto sem id", null!, null!];
        yield return ["prato sem id", null!, null!];
        yield return ["fornecedor sem produto", null!, null!];
        yield return ["texto livre desconhecido", ProductContext(), null!];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Parent_agent_routes_only_safe_domains(string message, AssistantContextResponse? context, string? expectedTool)
    {
        var intent = _coordinator.Resolve(message, context);
        if (expectedTool is null)
        {
            intent.Should().BeNull();
            return;
        }

        intent.Should().NotBeNull();
        intent!.ToolName.Should().Be(expectedTool);
        intent.Specialist.Should().NotBeNullOrWhiteSpace();
    }

    private static AssistantContextResponse ProductContext() => new(11, "StockBelowMinimum", "Warning", "Estoque baixo", "Saldo baixo", "Product", 42, "Arroz", new Dictionary<string, string> { ["unidade"] = "kg" }, DateTime.UtcNow);
    private static AssistantContextResponse DishContext() => new(12, "DishCmvDefasagem", "Warning", "CMV alto", "Custo alto", "Dish", 9, "Prato executivo", new Dictionary<string, string>(), DateTime.UtcNow);
}
