using System.Text.Json;
using ISM.Application.Agents;
using ISM.Application.Common;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Finance;
using ISM.Domain.Modules.Orders;
using ISM.Domain.Modules.Stock.Enums;

namespace ISM.Application.Services.Agents;

public sealed record StockReplenishmentToolOutput(
    int ProductId,
    string ProductName,
    decimal CurrentQuantity,
    decimal DailyConsumption,
    decimal DaysRemaining,
    decimal SuggestedQuantity,
    bool OpenReplenishmentDataAvailable,
    string Recommendation);

public sealed class StockReplenishmentTool(IProductRepository products, IStockMovementRepository movements) : ReadOnlyAgentTool, IAgentTool
{
    private static readonly StockMovementType[] ConsumptionTypes = Enum.GetValues<StockMovementType>();
    public AgentToolDescriptor Descriptor { get; } = new("stock.analyzeReplenishment", "stock", "2.0", "Calcular risco de ruptura e quantidade sugerida sem comprar", AgentToolOperation.Read, ["Admin", "Manager"], 1, 5000, false, "medium") { InputSchema = new Dictionary<string, string> { ["productId"] = "integer > 0" } };

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!IsSafeObject(input))
            return Result(Descriptor, input.ValueKind == JsonValueKind.Object ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData, null, "A entrada deve ser um objeto sem tenant informado pelo solicitante.", context);
        if (!TryGetId(input, "productId", out var productId))
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "productId é obrigatório.", context);

        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Produto não encontrado.", context);
        if (product.RestaurantId != context.RestaurantId)
            return Result(Descriptor, AgentDecisionCode.Denied, null, "Produto fora do tenant autenticado.", context);

        var since = DateTime.UtcNow.AddDays(-30);
        var history = await movements.GetByProductIdAsync(context.RestaurantId, product.Id, since, cancellationToken);
        var consumption = history
            .Where(m => ConsumptionTypes.Contains(m.MovementType) && m.QuantityDelta < 0)
            .Sum(m => -m.QuantityDelta) / 30m;
        if (consumption <= 0m)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há consumo negativo confiável nos últimos 30 dias.", context);

        var days = product.CurrentQuantity / consumption;
        var hasOpenOrders = false;
        var output = new StockReplenishmentToolOutput(
            product.Id,
            product.Name,
            product.CurrentQuantity,
            decimal.Round(consumption, 3),
            decimal.Round(days, 2),
            decimal.Round(Math.Max(0m, product.MaximumQuantity - product.CurrentQuantity), 3),
            hasOpenOrders,
            hasOpenOrders ? "Revisar pedido aberto antes de repor." : "Não é seguro sugerir compra: pedidos de reposição abertos não estão disponíveis.");

        var code = hasOpenOrders || days > 2m ? AgentDecisionCode.CanAct : AgentDecisionCode.InsufficientData;
        var message = code == AgentDecisionCode.CanAct ? "Risco de estoque calculado." : "Risco calculado, mas faltam dados de pedidos de reposição.";
        return Result(Descriptor, code, output, message, context, [new("StockMovement", product.Id.ToString(), context.RestaurantId, DateTime.UtcNow, since, "Consumo negativo escopado ao tenant nos últimos 30 dias.")]);
    }
}

public sealed record DishPriceSimulationToolOutput(
    int DishId,
    string DishName,
    decimal VerifiedCost,
    decimal CurrentPrice,
    decimal CurrentCmvPercent,
    decimal TargetMarginPercent,
    decimal SuggestedPrice,
    decimal PriceDelta);

public sealed class DishPriceSimulationTool(IDishRepository dishes) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("pricing.simulateDishPrice", "pricing", "2.0", "Simular preço e CMV determinísticos sem alterar o cardápio", AgentToolOperation.Read, ["Admin", "Manager"], 1, 5000, false, "medium") { InputSchema = new Dictionary<string, string> { ["dishId"] = "integer > 0", ["targetMarginPercent"] = "optional number between 0 and 100" } };

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!IsSafeObject(input))
            return Result(Descriptor, input.ValueKind == JsonValueKind.Object ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData, null, "A entrada deve ser um objeto sem tenant informado pelo solicitante.", context);
        if (!TryGetId(input, "dishId", out var dishId))
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "dishId é obrigatório.", context);
        var dish = await dishes.GetDishByIdAsync(dishId, cancellationToken);
        if (dish is null)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Prato não encontrado.", context);
        if (dish.RestaurantId != context.RestaurantId)
            return Result(Descriptor, AgentDecisionCode.Denied, null, "Prato fora do tenant autenticado.", context);

        var target = dish.TargetMarginPercent ?? 60m;
        if (input.TryGetProperty("targetMarginPercent", out var targetElement))
        {
            if (targetElement.ValueKind != JsonValueKind.Number || !targetElement.TryGetDecimal(out target))
                return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "targetMarginPercent deve ser numérico.", context);
        }
        if (target <= 0m || target >= 100m || dish.Price <= 0m || dish.Ingredients.Count == 0 || dish.Ingredients.Any(i => i.Product is null || i.Product.AverageCost < 0m))
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Ficha técnica, custos ou margem alvo não são confiáveis para simulação.", context);

        var cost = dish.Ingredients.Sum(i => i.Quantity * i.Product!.AverageCost);
        var suggested = cost / (1m - target / 100m);
        var output = new DishPriceSimulationToolOutput(dish.Id, dish.Name, decimal.Round(cost, 2), dish.Price, decimal.Round(cost / dish.Price * 100m, 2), target, decimal.Round(suggested, 2), decimal.Round(suggested - dish.Price, 2));
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Simulação determinística concluída; nenhum preço foi alterado.", context, [new("Dish", dish.Id.ToString(), context.RestaurantId, DateTime.UtcNow, dish.UpdatedAtUtc, "Ficha técnica e custos médios atuais do prato.")]);
    }
}

public sealed record SupplierOfferComparison(string Supplier, int? SupplierId, string Unit, decimal WeightedUnitPrice, DateTime LastPurchaseUtc, bool IsStale);
public sealed record SupplierComparisonToolOutput(string ProductName, string Unit, int LookbackDays, IReadOnlyList<SupplierOfferComparison> Offers);

public sealed class SupplierComparisonTool(ISupplierProductPriceHistoryRepository historyRepository) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("suppliers.compareOffers", "suppliers", "2.0", "Comparar histórico de preços de compras registradas; não consulta cotações atuais", AgentToolOperation.Read, ["Admin", "Manager"], 20, 5000, false, "medium") { InputSchema = new Dictionary<string, string> { ["productName"] = "non-empty string", ["unit"] = "non-empty string", ["lookbackDays"] = "optional integer 1..365" } };

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!IsSafeObject(input))
            return Result(Descriptor, input.ValueKind == JsonValueKind.Object ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData, null, "A entrada deve ser um objeto sem tenant informado pelo solicitante.", context);
        if (!input.TryGetProperty("productName", out var nameElement) || nameElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameElement.GetString())
            || !input.TryGetProperty("unit", out var unitElement) || unitElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(unitElement.GetString()))
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "productName e unit são obrigatórios.", context);

        var lookbackDays = 60;
        if (input.TryGetProperty("lookbackDays", out var daysElement) && (daysElement.ValueKind != JsonValueKind.Number || !daysElement.TryGetInt32(out lookbackDays) || lookbackDays is < 1 or > 365))
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "lookbackDays deve estar entre 1 e 365.", context);

        var normalizedProduct = StringHelper.NormalizeName(nameElement.GetString());
        var normalizedUnit = unitElement.GetString()!.Trim().ToLowerInvariant();
        var rows = await historyRepository.GetForRestaurantSinceAsync(context.RestaurantId, DateTime.UtcNow.AddDays(-lookbackDays), cancellationToken);
        var offers = rows.Where(row => StringHelper.NormalizeName(row.ProductRawName) == normalizedProduct && row.Unit.Trim().ToLowerInvariant() == normalizedUnit && row.UnitPrice > 0m)
            .GroupBy(row => new { Supplier = StringHelper.NormalizeName(row.SupplierRawName), row.SupplierId, row.Unit })
            .Select(group => new SupplierOfferComparison(group.OrderBy(x => x.PurchasedAtUtc).Last().SupplierRawName, group.Key.SupplierId, group.Key.Unit, decimal.Round(group.Sum(x => x.UnitPrice * x.QuantityPurchased) / Math.Max(0.001m, group.Sum(x => x.QuantityPurchased)), 2), group.Max(x => x.PurchasedAtUtc), false))
            .Select(offer => offer with { IsStale = offer.LastPurchaseUtc < DateTime.UtcNow.AddDays(-Math.Min(lookbackDays, 30)) })
            .Where(offer => !offer.IsStale)
            .OrderBy(offer => offer.WeightedUnitPrice)
            .Take(20)
            .ToArray();
        if (offers.Length == 0)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há ofertas equivalentes e recentes para comparar.", context);
        return Result(Descriptor, AgentDecisionCode.CanAct, new SupplierComparisonToolOutput(nameElement.GetString()!.Trim(), normalizedUnit, lookbackDays, offers), "Histórico de preços de compras anteriores consultado; estes valores não são cotações atuais.", context, [new("SupplierPriceHistory", normalizedProduct, context.RestaurantId, DateTime.UtcNow, DateTime.UtcNow.AddDays(-lookbackDays), "Histórico de preços do tenant, com unidade compatível e validade verificada.")]);
    }
}

public sealed record FinancePeriodToolOutput(DateTime StartUtc, DateTime EndUtc, decimal PaidSalesRevenue, decimal SettledOtherIncome, decimal SettledExpenses, decimal NetCashFlow, int PaidOrders, int FinanceEntries);

public sealed class FinancePeriodTool(IFinanceEntryRepository entries, IOrderRepository orders) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("finance.getPeriodSummary", "finance", "2.0", "Consultar receita, despesas e fluxo de caixa de um período", AgentToolOperation.Read, ["Admin", "Manager"], 1, 5000, false, "medium") { InputSchema = new Dictionary<string, string> { ["startUtc"] = "ISO-8601 UTC datetime", ["endUtc"] = "ISO-8601 UTC datetime" } };

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!IsSafeObject(input))
            return Result(Descriptor, input.ValueKind == JsonValueKind.Object ? AgentDecisionCode.Denied : AgentDecisionCode.InsufficientData, null, "A entrada deve ser um objeto sem tenant informado pelo solicitante.", context);
        if (!TryGetDate(input, "startUtc", out var start) || !TryGetDate(input, "endUtc", out var end) || end <= start || (end - start).TotalDays > 366)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "startUtc e endUtc devem formar um período UTC válido de até 366 dias.", context);

        var allEntries = await entries.GetAllAsync(context.RestaurantId, cancellationToken);
        var allOrders = await orders.GetAllAsync(context.RestaurantId, cancellationToken);
        var periodEntries = allEntries.Where(entry => entry.SettledAtUtc.HasValue && entry.SettledAtUtc.Value >= start && entry.SettledAtUtc.Value <= end).ToArray();
        var paidOrders = allOrders.Where(order => order.Status == OrderStatus.Paid && order.PaidAtUtc.HasValue && order.PaidAtUtc.Value >= start && order.PaidAtUtc.Value <= end).ToArray();
        var sales = paidOrders.SelectMany(order => order.Items).Sum(item => item.UnitPrice * item.Quantity);
        var income = periodEntries.Where(entry => entry.Type == FinanceEntryType.Income).Sum(entry => entry.Amount);
        var expenses = periodEntries.Where(entry => entry.Type == FinanceEntryType.Expense).Sum(entry => entry.Amount);
        if (periodEntries.Length == 0 && paidOrders.Length == 0)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há pedidos pagos ou lançamentos liquidados no período informado.", context);

        var output = new FinancePeriodToolOutput(start, end, sales, income, expenses, sales + income - expenses, paidOrders.Length, periodEntries.Length);
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Resumo financeiro calculado por período e fonte efetiva.", context, [new("FinancePeriod", $"{start:O}/{end:O}", context.RestaurantId, DateTime.UtcNow, end, "Pedidos pagos e lançamentos liquidados do período.")]);
    }

    private static bool TryGetDate(JsonElement input, string property, out DateTime value)
    {
        value = default;
        return input.ValueKind == JsonValueKind.Object && input.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), null, global::System.Globalization.DateTimeStyles.AdjustToUniversal, out value);
    }
}
