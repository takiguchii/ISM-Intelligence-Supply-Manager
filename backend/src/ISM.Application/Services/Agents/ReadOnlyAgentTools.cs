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

public sealed record LatestOrderOutput(int OrderId, string Status, DateTime CreatedAtUtc, DateTime? PaidAtUtc, decimal Total, IReadOnlyList<string> Items);
public sealed class LatestOrderTool(IOrderRepository orders) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("orders.getLatest", "assistant", "1.0", "Consultar o pedido mais recente do restaurante", AgentToolOperation.Read, ["Admin", "Manager"], 1, 5000, false, "low");
    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        var latest = (await orders.GetAllAsync(context.RestaurantId, cancellationToken)).OrderByDescending(order => order.CreatedAtUtc).ThenByDescending(order => order.Id).FirstOrDefault();
        if (latest is null) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Ainda não há pedidos registrados neste restaurante.", context);
        var output = new LatestOrderOutput(latest.Id, latest.Status.ToString(), latest.CreatedAtUtc, latest.PaidAtUtc, latest.Items.Sum(item => item.Quantity * item.UnitPrice), latest.Items.Select(item => $"{item.Quantity}x {item.DishName}").ToArray());
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Pedido mais recente consultado.", context, [new("Order", latest.Id.ToString(), context.RestaurantId, DateTime.UtcNow, latest.CreatedAtUtc, "Pedido persistido no restaurante autenticado.")]);
    }
}

public sealed record RecentOrdersOutput(int Count, decimal PaidSales, IReadOnlyList<LatestOrderOutput> Orders);
public sealed class RecentOrdersTool(IOrderRepository orders) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("orders.getRecent", "finance", "1.0", "Consultar pedidos recentes e vendas pagas", AgentToolOperation.Read, ["Admin", "Manager"], 10, 5000, false, "low");
    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        var recent = (await orders.GetAllAsync(context.RestaurantId, cancellationToken)).OrderByDescending(order => order.CreatedAtUtc).ThenByDescending(order => order.Id).Take(10).ToArray();
        if (recent.Length == 0) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Ainda não há pedidos registrados neste restaurante.", context);
        var mapped = recent.Select(order => new LatestOrderOutput(order.Id, order.Status.ToString(), order.CreatedAtUtc, order.PaidAtUtc, order.Items.Sum(item => item.Quantity * item.UnitPrice), order.Items.Select(item => $"{item.Quantity}x {item.DishName}").ToArray())).ToArray();
        return Result(Descriptor, AgentDecisionCode.CanAct, new RecentOrdersOutput(mapped.Length, mapped.Where(order => order.Status == "Paid").Sum(order => order.Total), mapped), "Pedidos recentes consultados; vendas consideram apenas pedidos pagos.", context, [new("Orders", string.Join(',', mapped.Select(order => order.OrderId)), context.RestaurantId, DateTime.UtcNow, null, "Até dez pedidos mais recentes do restaurante autenticado.")]);
    }
}

public sealed record StockOverviewItem(string Name, decimal Current, decimal Minimum, string Unit, decimal AverageCost, bool Critical);
public sealed record StockOverviewOutput(int ProductCount, int CriticalCount, IReadOnlyList<StockOverviewItem> Products);
public sealed class StockOverviewTool(IProductRepository products) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("stock.getOverview", "stock", "1.0", "Consultar visão geral e itens críticos do estoque", AgentToolOperation.Read, ["Admin", "Manager"], 50, 5000, false, "low");
    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        var (rows, count) = await products.GetPagedAsync(context.RestaurantId, 1, 50, cancellationToken: cancellationToken);
        if (count == 0) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há produtos cadastrados no estoque deste restaurante.", context);
        var mapped = rows.Select(product => new StockOverviewItem(product.Name, product.CurrentQuantity, product.MinimumQuantity, product.Unit, product.AverageCost, product.CurrentQuantity <= product.MinimumQuantity)).ToArray();
        return Result(Descriptor, AgentDecisionCode.CanAct, new StockOverviewOutput(count, mapped.Count(item => item.Critical), mapped), $"Consulta concluída: {count} produtos, {mapped.Count(item => item.Critical)} abaixo do mínimo; exibindo até 50.", context, [new("Products", $"restaurant:{context.RestaurantId}", context.RestaurantId, DateTime.UtcNow, null, "Produtos consultados com filtro do restaurante autenticado.")]);
    }
}

public sealed record DishOverviewItem(string Name, decimal Price, decimal Cost, decimal CmvPercent, decimal? TargetMarginPercent);
public sealed record DishOverviewOutput(int DishCount, IReadOnlyList<DishOverviewItem> Dishes);
public sealed class DishOverviewTool(IDishRepository dishes) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("pricing.getDishOverview", "pricing", "1.0", "Consultar preços, custos e CMV dos pratos", AgentToolOperation.Read, ["Admin", "Manager"], 50, 5000, false, "low");
    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        var rows = (await dishes.GetAllDishesAsync(context.RestaurantId, cancellationToken: cancellationToken)).Where(dish => dish.IsActive).ToArray();
        if (rows.Length == 0) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há pratos ativos cadastrados neste restaurante.", context);
        var mapped = rows.Take(50).Select(dish => new DishOverviewItem(dish.Name, dish.Price, dish.Cost, dish.Price > 0 ? decimal.Round(dish.Cost / dish.Price * 100m, 2) : 0m, dish.TargetMarginPercent)).ToArray();
        return Result(Descriptor, AgentDecisionCode.CanAct, new DishOverviewOutput(rows.Length, mapped), $"Consulta concluída: {rows.Length} pratos ativos; exibindo até 50.", context, [new("Dishes", $"restaurant:{context.RestaurantId}", context.RestaurantId, DateTime.UtcNow, null, "Pratos ativos consultados com filtro do restaurante autenticado.")]);
    }
}
