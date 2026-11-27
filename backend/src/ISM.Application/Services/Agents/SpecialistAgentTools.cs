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

public sealed record FinanceDayBreakdown(string Date, string DayOfWeek, decimal SalesRevenue, decimal Expenses, int OrderCount);
public sealed record FinanceDayHighlight(string Date, string DayOfWeek, decimal SalesRevenue, int OrderCount);

public sealed record FinancePeriodToolOutput(
    DateTime StartUtc,
    DateTime EndUtc,
    decimal PaidSalesRevenue,
    decimal SettledOtherIncome,
    decimal SettledExpenses,
    decimal NetCashFlow,
    int PaidOrders,
    int FinanceEntries,
    decimal AverageDailySales,
    FinanceDayHighlight? BestSalesDay,
    FinanceDayHighlight? LowestSalesDay,
    IReadOnlyList<FinanceDayBreakdown> DailyBreakdown);

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

        var dailySalesGroup = paidOrders
            .GroupBy(o => o.PaidAtUtc!.Value.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    Revenue = g.SelectMany(o => o.Items).Sum(i => i.Quantity * i.UnitPrice)
                });

        var dailyExpenseGroup = periodEntries
            .Where(e => e.Type == FinanceEntryType.Expense && e.SettledAtUtc.HasValue)
            .GroupBy(e => e.SettledAtUtc!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var ptBr = new global::System.Globalization.CultureInfo("pt-BR");
        var daysSpan = Math.Max(1, (int)(end.Date - start.Date).TotalDays + 1);
        var dailyList = new List<FinanceDayBreakdown>();
        for (var offset = 0; offset < daysSpan; offset++)
        {
            var d = start.Date.AddDays(offset);
            if (d > end.Date) break;
            var dayRevenue = dailySalesGroup.TryGetValue(d, out var s) ? s.Revenue : 0m;
            var dayOrders = s?.Count ?? 0;
            var dayExpenses = dailyExpenseGroup.TryGetValue(d, out var exp) ? exp : 0m;
            if (dayRevenue > 0m || dayExpenses > 0m || dayOrders > 0)
            {
                var dayName = ptBr.DateTimeFormat.GetDayName(d.DayOfWeek);
                dayName = char.ToUpper(dayName[0]) + dayName[1..];
                dailyList.Add(new FinanceDayBreakdown(d.ToString("yyyy-MM-dd"), dayName, decimal.Round(dayRevenue, 2), decimal.Round(dayExpenses, 2), dayOrders));
            }
        }

        FinanceDayHighlight? bestDay = null;
        FinanceDayHighlight? lowestDay = null;
        if (dailyList.Count > 0)
        {
            var withSales = dailyList.Where(d => d.SalesRevenue > 0).ToList();
            if (withSales.Count > 0)
            {
                var best = withSales.OrderByDescending(d => d.SalesRevenue).First();
                bestDay = new FinanceDayHighlight(best.Date, best.DayOfWeek, best.SalesRevenue, best.OrderCount);
                var lowest = withSales.OrderBy(d => d.SalesRevenue).First();
                lowestDay = new FinanceDayHighlight(lowest.Date, lowest.DayOfWeek, lowest.SalesRevenue, lowest.OrderCount);
            }
        }

        var totalDays = Math.Max(1, dailyList.Count > 0 ? dailyList.Count : (int)(end - start).TotalDays);
        var avgDaily = decimal.Round(sales / totalDays, 2);

        var output = new FinancePeriodToolOutput(start, end, decimal.Round(sales, 2), decimal.Round(income, 2), decimal.Round(expenses, 2), decimal.Round(sales + income - expenses, 2), paidOrders.Length, periodEntries.Length, avgDaily, bestDay, lowestDay, dailyList);
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Resumo financeiro calculado por período e fonte efetiva.", context, [new("FinancePeriod", $"{start:O}/{end:O}", context.RestaurantId, DateTime.UtcNow, end, "Pedidos pagos e lançamentos liquidados do período.")]);
    }

    private static bool TryGetDate(JsonElement input, string property, out DateTime value)
    {
        value = default;
        return input.ValueKind == JsonValueKind.Object && input.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), null, global::System.Globalization.DateTimeStyles.AdjustToUniversal, out value);
    }
}

public sealed record SalesRankingItem(string Dish, int Quantity, decimal Revenue, decimal Profit);
public sealed record SalesInsightsOutput(DateTime StartUtc, DateTime EndUtc, int PaidOrders, IReadOnlyList<SalesRankingItem> Ranking, IReadOnlyList<SalesRankingItem> LeastSold, string Metric, bool IsForecast, string? ForecastNote);

public sealed class SalesInsightsTool(IOrderRepository orders) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("sales.getDishInsights", "finance", "2.0", "Consultar pratos mais/menos vendidos, lucro realizado por prato ou estimar demanda com base em pedidos pagos", AgentToolOperation.Read, ["Admin", "Manager"], 20, 5000, false, "medium") { InputSchema = new Dictionary<string, string> { ["startUtc"] = "ISO-8601 UTC datetime", ["endUtc"] = "ISO-8601 UTC datetime", ["forecast"] = "boolean", ["profit"] = "boolean", ["includeLeast"] = "boolean" } };

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!IsSafeObject(input) || !TryDate(input, "startUtc", out var start) || !TryDate(input, "endUtc", out var end) || end <= start || (end-start).TotalDays > 366)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Informe um período UTC válido de até 366 dias.", context);
        var forecast = input.TryGetProperty("forecast", out var forecastElement) && forecastElement.ValueKind == JsonValueKind.True;
        var profit = input.TryGetProperty("profit", out var profitElement) && profitElement.ValueKind == JsonValueKind.True;
        var includeLeast = input.TryGetProperty("includeLeast", out var leastElement) && leastElement.ValueKind == JsonValueKind.True;
        var all = await orders.GetAllAsync(context.RestaurantId, cancellationToken);
        var paid = all.Where(o => o.Status == OrderStatus.Paid && o.PaidAtUtc.HasValue && o.PaidAtUtc.Value >= start && o.PaidAtUtc.Value <= end).ToArray();
        if (paid.Length == 0) return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há pedidos pagos no período informado.", context);
        var ranked = paid.SelectMany(o => o.Items).GroupBy(i => i.DishName).Select(g => new SalesRankingItem(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.Quantity * i.UnitPrice), g.Sum(i => i.Quantity * (i.UnitPrice - i.UnitCost)))).ToArray();
        var minQuantity = ranked.Min(i => i.Quantity);
        var leastSold = includeLeast ? ranked.Where(i => i.Quantity == minQuantity).ToArray() : [];
        var note = forecast ? "Estimativa de referência: repete o volume observado nos últimos 7 dias para os próximos 7, assumindo padrão semelhante; não modela sazonalidade, eventos ou mudanças futuras." : null;
        if (forecast)
        {
            var recentStart = end.AddDays(-7);
            var recent = paid.Where(o => o.PaidAtUtc >= recentStart).SelectMany(o => o.Items).GroupBy(i => i.DishName)
                .ToDictionary(g => g.Key, g => new { Units = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Quantity * i.UnitPrice), Profit = g.Sum(i => i.Quantity * (i.UnitPrice - i.UnitCost)) });
            ranked = recent.Select(pair => new SalesRankingItem(pair.Key,
                pair.Value.Units, decimal.Round(pair.Value.Revenue, 2), decimal.Round(pair.Value.Profit, 2))).ToArray();
        }
        var ordered = (profit ? ranked.OrderByDescending(i => i.Profit) : ranked.OrderByDescending(i => i.Quantity)).Take(10).ToArray();
        if (forecast) leastSold = [];
        var output = new SalesInsightsOutput(start, end, paid.Length, ordered, leastSold, profit ? "profit" : "quantity", forecast, note);
        var message = forecast ? "Estimativa de demanda baseada nos itens de pedidos pagos recentes." : profit ? "Lucro bruto realizado calculado como preço vendido menos custo registrado nos itens pagos." : "Ranking de quantidades e receita dos itens de pedidos pagos.";
        return Result(Descriptor, AgentDecisionCode.CanAct, output, message, context, [new("PaidOrders", $"{start:O}/{end:O}", context.RestaurantId, DateTime.UtcNow, end, "Itens agregados por prato em pedidos pagos do restaurante autenticado.")]);
    }
    private static bool TryDate(JsonElement input, string key, out DateTime value)
    {
        value = default;
        return input.TryGetProperty(key, out var e) && e.ValueKind == JsonValueKind.String && DateTime.TryParse(e.GetString(), null, global::System.Globalization.DateTimeStyles.AdjustToUniversal, out value);
    }
}

public sealed record RevenueForecastOutput(decimal Last7Days, decimal Previous7Days, decimal WeeklyChangePercent, decimal Next7DaysEstimate, int PaidOrdersLast28Days, string Method);

public sealed class RevenueForecastTool(IOrderRepository orders) : ReadOnlyAgentTool, IAgentTool
{
    public AgentToolDescriptor Descriptor { get; } = new("finance.forecastRevenue", "finance", "1.0", "Estimar receita de pedidos pagos para os próximos sete dias usando comparação semanal", AgentToolOperation.Read, ["Admin", "Manager"], 1, 5000, false, "medium");

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        if (!IsSafeObject(input)) return Result(Descriptor, AgentDecisionCode.Denied, null, "Entrada inválida.", context);
        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-28);
        var ordersInRange = (await orders.GetAllAsync(context.RestaurantId, cancellationToken))
            .Where(o => o.Status == OrderStatus.Paid && o.PaidAtUtc >= start && o.PaidAtUtc < end).ToArray();
        var daily = ordersInRange.SelectMany(o => o.Items.Select(i => (Date: o.PaidAtUtc!.Value.Date, Revenue: i.Quantity * i.UnitPrice)))
            .GroupBy(i => i.Date).ToDictionary(g => g.Key, g => g.Sum(i => i.Revenue));
        decimal SumDays(DateTime from, int days) => Enumerable.Range(0, days).Sum(i => daily.GetValueOrDefault(from.AddDays(i)));
        var last = SumDays(end.AddDays(-7), 7);
        var previous = SumDays(end.AddDays(-14), 7);
        if (ordersInRange.Length == 0 || last + previous <= 0m)
            return Result(Descriptor, AgentDecisionCode.InsufficientData, null, "Não há vendas pagas suficientes nos últimos 14 dias para projetar o faturamento.", context);
        var change = previous > 0m ? (last - previous) / previous * 100m : 0m;
        var estimate = Math.Max(0m, last * (1m + Math.Clamp(change, -50m, 50m) / 100m));
        var output = new RevenueForecastOutput(decimal.Round(last, 2), decimal.Round(previous, 2), decimal.Round(change, 2), decimal.Round(estimate, 2), ordersInRange.Length,
            "Compara duas janelas de sete dias e aplica a variação observada, limitada a +/-50%; não modela sazonalidade.");
        return Result(Descriptor, AgentDecisionCode.CanAct, output, "Estimativa informativa baseada nas vendas pagas recentes; não é garantia de resultado.", context,
            [new("PaidOrders", $"{start:yyyy-MM-dd}/{end:yyyy-MM-dd}", context.RestaurantId, DateTime.UtcNow, end, "Receita dos itens de pedidos pagos nos últimos 28 dias." )]);
    }
}
