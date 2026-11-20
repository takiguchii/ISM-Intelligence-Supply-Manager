using System.Text.Json;
using ISM.Application.Agents;
using ISM.Application.Interfaces.Finance;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Agents;

public abstract class ReadOnlyAgentTool
{
    protected static bool IsSafeObject(JsonElement input)
        => input.ValueKind == JsonValueKind.Object && !input.TryGetProperty("restaurantId", out _);

    protected static AgentToolResult Result(AgentToolDescriptor descriptor, AgentDecisionCode code, object? data, string message, AgentToolContext context, IReadOnlyList<EvidenceRef> evidence = null!)
        => new(descriptor.ToolName, code, data, evidence ?? [], message, context.CorrelationId, 0);

    protected static bool TryGetId(JsonElement input, string property, out int id)
    {
        id = 0;
        return input.ValueKind == JsonValueKind.Object
            && input.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out id)
            && id > 0;
    }
}

public sealed record StockProductToolOutput(int Id, string Name, decimal CurrentQuantity, decimal MinimumQuantity, decimal AverageCost, string Unit, bool IsCritical);

public sealed class StockProductTool(IProductRepository products) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("stock.getProduct", "stock", "1.0", "Consultar saldo e risco de um produto", AgentToolOperation.Read, ["Admin", "Manager"], 1, 3000, false, "low");

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetId(input, "productId", out var productId)) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "productId é obrigatório.", context);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Produto não encontrado.", context);
        if (product.RestaurantId != context.RestaurantId) return Result(Descriptor, AgentDecisionCode.Denied, null, "Produto fora do tenant autenticado.", context);
        var output = new StockProductToolOutput(product.Id, product.Name, product.CurrentQuantity, product.MinimumQuantity, product.AverageCost, product.Unit, product.CurrentQuantity <= product.MinimumQuantity);
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Produto consultado.", context, [new("Product", product.Id.ToString(), context.RestaurantId, DateTime.UtcNow, product.UpdatedAtUtc, "Saldo e mínimo autorizados do produto.")]);
    }
}

public sealed record FinanceSummaryToolOutput(decimal SalesRevenue, decimal OtherIncome, decimal Expenses, decimal AccountsReceivable, decimal AccountsPayable);

public sealed class FinanceSummaryTool(IFinanceService finance) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("finance.getRevenueSummary", "finance", "1.0", "Consultar resumo financeiro do mês corrente", AgentToolOperation.Read, ["Admin", "Manager"], 1, 5000, false, "medium");

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        var overview = await finance.GetAsync(1, 1, cancellationToken);
        var summary = overview.Summary;
        var output = new FinanceSummaryToolOutput(summary.SalesRevenue, summary.OtherIncome, summary.Expenses, summary.AccountsReceivable, summary.AccountsPayable);
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Resumo financeiro consultado por fonte autorizada.", context, [new("FinanceOverview", $"restaurant:{context.RestaurantId}", context.RestaurantId, DateTime.UtcNow, DateTime.UtcNow, "Pedidos pagos e lançamentos financeiros do mês corrente.")]);
    }
}

public sealed record DishCostToolOutput(int Id, string Name, decimal Price, decimal Cost, decimal? TargetMarginPercent, int PriceRevision);

public sealed class DishCostTool(IDishRepository dishes) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("pricing.getDishCost", "pricing", "1.0", "Consultar preço, custo e margem de um prato", AgentToolOperation.Read, ["Admin", "Manager"], 1, 3000, false, "low");

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetId(input, "dishId", out var dishId)) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "dishId é obrigatório.", context);
        var dish = await dishes.GetDishByIdAsync(dishId, cancellationToken);
        if (dish is null) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Prato não encontrado.", context);
        if (dish.RestaurantId != context.RestaurantId) return Result(Descriptor, AgentDecisionCode.Denied, null, "Prato fora do tenant autenticado.", context);
        return Result(Descriptor, AgentDecisionCode.CanAct, new DishCostToolOutput(dish.Id, dish.Name, dish.Price, dish.Cost, dish.TargetMarginPercent, dish.PriceRevision), "Custo do prato consultado.", context, [new("Dish", dish.Id.ToString(), context.RestaurantId, DateTime.UtcNow, dish.UpdatedAtUtc, "Preço, custo e revisão atuais do prato.")]);
    }
}

public sealed record AlertContextToolOutput(int Id, string Title, string Message, string Severity, string AlertType, DateTime GeneratedAtUtc);

public sealed class AlertContextTool(ISystemAlertRepository alerts) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("assistant.getAlertContext", "assistant", "1.0", "Consultar fatos de um alerta do tenant", AgentToolOperation.Read, ["Admin", "Manager"], 1, 3000, false, "low");

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetId(input, "alertId", out var alertId)) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "alertId é obrigatório.", context);
        var alert = await alerts.GetByIdAsync(alertId, cancellationToken);
        if (alert is null) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Alerta não encontrado.", context);
        if (alert.RestaurantId != context.RestaurantId) return Result(Descriptor, AgentDecisionCode.Denied, null, "Alerta fora do tenant autenticado.", context);
        return Result(Descriptor, AgentDecisionCode.CanAct, new AlertContextToolOutput(alert.Id, alert.Title, alert.Message, alert.Severity.ToString(), alert.AlertType.ToString(), alert.GeneratedAtUtc), "Alerta consultado.", context, [new("SystemAlert", alert.Id.ToString(), context.RestaurantId, DateTime.UtcNow, alert.GeneratedAtUtc, "Alerta persistido e escopado ao restaurante.")]);
    }
}
