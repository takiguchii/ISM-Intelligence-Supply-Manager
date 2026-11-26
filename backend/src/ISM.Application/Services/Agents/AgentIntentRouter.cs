using System.Text.Json;
using System.Text.RegularExpressions;
using ISM.Application.Agents;

namespace ISM.Application.Services.Agents;

public sealed class AgentIntentRouter : IAgentIntentRouter
{
    private static readonly IReadOnlyDictionary<string, (string Specialist, string[] Arguments)> ToolSchema
        = new Dictionary<string, (string, string[])>(StringComparer.Ordinal)
        {
            ["finance.getRevenueSummary"] = ("finance", Array.Empty<string>()),
            ["finance.getPeriodSummary"] = ("finance", ["startUtc", "endUtc"]),
            ["sales.getDishInsights"] = ("finance", ["startUtc", "endUtc", "forecast", "profit", "includeLeast"]),
            ["finance.forecastRevenue"] = ("finance", Array.Empty<string>()),
            ["stock.getProduct"] = ("stock", ["productId"]),
            ["stock.analyzeReplenishment"] = ("stock", ["productId"]),
            ["pricing.getDishCost"] = ("pricing", ["dishId"]),
            ["pricing.simulateDishPrice"] = ("pricing", ["dishId"]),
            ["suppliers.compareOffers"] = ("suppliers", ["productName", "unit"]),
            ["assistant.getAlertContext"] = ("assistant", ["alertId"]),
            ["orders.getLatest"] = ("assistant", Array.Empty<string>()),
            ["orders.getRecent"] = ("finance", Array.Empty<string>()),
            ["stock.getOverview"] = ("stock", Array.Empty<string>()),
            ["pricing.getDishOverview"] = ("pricing", Array.Empty<string>())
        };

    public AgentIntent? Resolve(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        var normalized = message.Trim().ToLowerInvariant();
        if (new[] { "previsão de faturamento", "previsao de faturamento", "previsão de receita", "previsao de receita", "estimar o faturamento", "estime a receita" }.Any(normalized.Contains))
            return Intent("finance", "finance.forecastRevenue", new { }, 0.96);
        var asksDishSales = new[] { "mais vendido", "mais vendidos", "menos vendido", "menos vendidos", "mais vendeu", "mais venderam", "ranking de vendas", "lucro por prato", "lucro dos pratos", "deu lucro", "deu mais lucro", "previsão de vendas", "previsao de vendas", "previsão da semana", "previsao da semana", "próxima semana", "proxima semana" }.Any(normalized.Contains);
        var asksNaturalDishForecast = (normalized.Contains("prato") || normalized.Contains("pratos"))
            && new[] { "previs", "acha", "vai vender", "vender mais", "boa previsão", "boa previsao" }.Any(normalized.Contains);
        if (asksDishSales || asksNaturalDishForecast)
        {
            var end = DateTime.UtcNow; var start = end.AddDays(-7);
            if (normalized.Contains("mês") || normalized.Contains("mes")) start = new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            else if (normalized.Contains("dia") || normalized.Contains("hoje")) start = end.Date;
            else if (normalized.Contains("semana") && !normalized.Contains("próxima") && !normalized.Contains("proxima")) start = end.AddDays(-7);
            var forecast = normalized.Contains("previs") || normalized.Contains("próxima semana") || normalized.Contains("proxima semana") || normalized.Contains("vai vender");
            var profit = new[] { "lucro", "lucrativo", "lucratividade", "ganho" }.Any(normalized.Contains);
            var includeLeast = normalized.Contains("menos vendido") || normalized.Contains("menos vendidos") || normalized.Contains("menos lucr");
            return Intent("finance", "sales.getDishInsights", new { startUtc = start, endUtc = end, forecast, profit, includeLeast }, 0.97);
        }
        if (TryReadId(normalized, "produto", out var replenishmentProductId)
            && new[] { "repor", "reposição", "reposicao", "ruptura", "consumo médio", "consumo medio", "dias restantes", "consumo e os dias" }.Any(normalized.Contains))
            return Intent("stock", "stock.analyzeReplenishment", new { productId = replenishmentProductId }, 0.98);
        if (TryReadId(normalized, "prato", out var simulationDishId)
            && new[] { "simul", "margem alvo", "preço sugerido", "preco sugerido", "ajustar o preço", "ajustar o preco" }.Any(normalized.Contains))
            return Intent("pricing", "pricing.simulateDishPrice", new { dishId = simulationDishId }, 0.98);
        if (TryReadId(normalized, "prato", out var costDishId)
            && new[] { "margem", "custo", "cmv", "preço atual", "preco atual" }.Any(normalized.Contains))
            return Intent("pricing", "pricing.getDishCost", new { dishId = costDishId }, 0.96);
        if (TryResolveSupplierComparison(normalized, out var supplierProduct, out var supplierUnit))
            return Intent("suppliers", "suppliers.compareOffers", new { productName = supplierProduct, unit = supplierUnit }, 0.96);
        if (new[] { "abaixo do mínimo", "abaixo do minimo", "itens críticos", "itens criticos", "estoque crítico", "estoque critico", "estoque geral", "visão geral do estoque", "visao geral do estoque" }.Any(normalized.Contains))
            return Intent("stock", "stock.getOverview", new { }, 0.96);
        if (TryResolveFinancePeriod(normalized, out var startUtc, out var endUtc))
            return Intent("finance", "finance.getPeriodSummary", new { startUtc, endUtc }, 0.96);
        if (normalized.Contains("último pedido") || normalized.Contains("ultimo pedido") || normalized.Contains("pedido mais recente"))
            return Intent("assistant", "orders.getLatest", new { }, 0.98);
        if (normalized.Contains("pedidos recentes") || normalized.Contains("pedidos pagos recentes") || normalized.Contains("vendas") || normalized.Contains("vendemos") || normalized.Contains("como estão as vendas") || normalized.Contains("como estao as vendas"))
            return Intent("finance", "orders.getRecent", new { }, 0.94);
        if (normalized.Contains("outros itens") || normalized.Contains("outros produtos") || normalized.Contains("visão geral do estoque") || normalized.Contains("visao geral do estoque") || normalized.Contains("estoque geral"))
            return Intent("stock", "stock.getOverview", new { }, 0.94);
        if (normalized.Contains("resumo dos pratos") || normalized.Contains("todos os pratos") || normalized.Contains("pratos ativos") || normalized.Contains("pratos cadastrados") || normalized.Contains("prato cadastrado") || normalized.Contains("liste pratos") || normalized.Contains("listar pratos") || normalized.Contains("situação dos pratos") || normalized.Contains("situacao dos pratos") || normalized.Contains("cardápio") || normalized.Contains("cardapio"))
            return Intent("pricing", "pricing.getDishOverview", new { }, 0.94);
        if (normalized.Contains("faturamento") || normalized.Contains("receita") || normalized.Contains("despesa") || normalized.Contains("fluxo de caixa") || normalized.Contains("situação financeira") || normalized.Contains("situacao financeira") || normalized.Contains("financeiro"))
            return Intent("finance", "finance.getRevenueSummary", new { }, 0.98);
        if (TryReadId(normalized, "produto", out var productId) && (normalized.Contains("estoque") || normalized.Contains("saldo") || normalized.Contains("produto")))
            return Intent("stock", "stock.getProduct", new { productId }, 0.94);
        if (TryReadId(normalized, "alerta", out var alertId))
            return Intent("assistant", "assistant.getAlertContext", new { alertId }, 0.91);
        if (TryReadId(normalized, "prato", out var dishId) && (normalized.Contains("custo") || normalized.Contains("cmv") || normalized.Contains("preço")))
            return Intent("pricing", "pricing.getDishCost", new { dishId }, 0.91);
        return null;
    }

    public AgentIntent? ResolveStructured(JsonElement modelOutput)
    {
        if (modelOutput.ValueKind != JsonValueKind.Object
            || !modelOutput.TryGetProperty("specialist", out var specialistElement)
            || !modelOutput.TryGetProperty("toolName", out var toolElement)
            || !modelOutput.TryGetProperty("arguments", out var argumentsElement)
            || specialistElement.ValueKind != JsonValueKind.String
            || toolElement.ValueKind != JsonValueKind.String
            || argumentsElement.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in modelOutput.EnumerateObject())
        {
            if (property.Name is not ("specialist" or "toolName" or "arguments" or "confidence"))
                return null;
        }

        var specialist = specialistElement.GetString();
        var toolName = toolElement.GetString();
        if (string.IsNullOrWhiteSpace(specialist)
            || string.IsNullOrWhiteSpace(toolName)
            || !ToolSchema.TryGetValue(toolName, out var schema)
            || !string.Equals(specialist, schema.Specialist, StringComparison.Ordinal))
            return null;

        var allowedArguments = schema.Arguments.ToHashSet(StringComparer.Ordinal);
        foreach (var property in argumentsElement.EnumerateObject())
        {
            if (!allowedArguments.Contains(property.Name) || property.Name.Equals("restaurantId", StringComparison.OrdinalIgnoreCase))
                return null;
        }

        foreach (var requiredArgument in schema.Arguments)
        {
            if (!argumentsElement.TryGetProperty(requiredArgument, out var value) || !IsValidArgument(requiredArgument, value))
                return null;
        }

        var confidence = 1d;
        if (modelOutput.TryGetProperty("confidence", out var confidenceElement))
        {
            if (confidenceElement.ValueKind != JsonValueKind.Number
                || !confidenceElement.TryGetDouble(out confidence)
                || confidence is < 0 or > 1)
                return null;
        }

        return new AgentIntent(specialist, toolName, argumentsElement.Clone(), confidence);
    }

    private static bool IsValidArgument(string name, JsonElement value)
        => name is "productId" or "dishId" or "alertId"
            ? value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var id) && id > 0
            : name is "forecast" or "profit" or "includeLeast" ? value.ValueKind is JsonValueKind.True or JsonValueKind.False
            : name is "startUtc" or "endUtc" ? value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out _)
            : value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());

    private static AgentIntent Intent(string specialist, string toolName, object input, double confidence)
        => new(specialist, toolName, JsonSerializer.SerializeToElement(input), confidence);

    private static bool TryReadId(string message, string label, out int id)
    {
        id = 0;
        var marker = message.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return false;
        var digits = new string(message[(marker + label.Length)..].TrimStart().TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out id) && id > 0;
    }

    private static bool TryResolveFinancePeriod(string message, out DateTime startUtc, out DateTime endUtc)
    {
        endUtc = DateTime.UtcNow;
        if (message.Contains("últimos 7 dias", StringComparison.OrdinalIgnoreCase) || message.Contains("ultimos 7 dias", StringComparison.OrdinalIgnoreCase))
        {
            startUtc = endUtc.AddDays(-7);
            return true;
        }
        if (message.Contains("últimos 30 dias", StringComparison.OrdinalIgnoreCase) || message.Contains("ultimos 30 dias", StringComparison.OrdinalIgnoreCase))
        {
            startUtc = endUtc.AddDays(-30);
            return true;
        }
        if (message.Contains("mês passado", StringComparison.OrdinalIgnoreCase) || message.Contains("mes passado", StringComparison.OrdinalIgnoreCase))
        {
            var firstCurrentMonth = new DateTime(endUtc.Year, endUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            startUtc = firstCurrentMonth.AddMonths(-1);
            endUtc = firstCurrentMonth.AddTicks(-1);
            return true;
        }
        if (message.Contains("este mês", StringComparison.OrdinalIgnoreCase) || message.Contains("este mes", StringComparison.OrdinalIgnoreCase) || message.Contains("neste mês", StringComparison.OrdinalIgnoreCase) || message.Contains("neste mes", StringComparison.OrdinalIgnoreCase))
        {
            startUtc = new DateTime(endUtc.Year, endUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }
        startUtc = default;
        return false;
    }

    private static bool TryResolveSupplierComparison(string message, out string productName, out string unit)
    {
        var patterns = new[]
        {
            @"(?:fornecedores?|ofertas?|cota(?:ç|c)(?:ã|a)o)\s+(?:de|do|da|para)\s+(?<product>.+?)\s+(?:em\s+)?(?<unit>kg|un|l|g)\b",
            @"(?:cota(?:ç|c)(?:ã|a)o|oferta)\s+(?:recente|atuais?|disponíveis?|disponiveis)\s+(?:de|do|da|para)\s+(?<product>.+?)\s+(?:em\s+)?(?<unit>kg|un|l|g)\b",
            @"quem\s+(?:vende|fornece)\s+(?<product>.+?)\s+(?:em\s+)?(?<unit>kg|un|l|g)\b",
            @"(?:menor\s+preço|menor\s+preco|preço\s+mais\s+baixo|preco\s+mais\s+baixo|preços\s+recentes|precos\s+recentes|preço\s+de\s+compra|preco\s+de\s+compra)\s+(?:do|da|de|para)\s+(?<product>.+?)\s+(?:em\s+)?(?<unit>kg|un|l|g)\b",
            @"fornecedores?\s+(?:oferecem|vendem)\s+(?<product>.+?)\s+(?:em\s+)?(?<unit>kg|un|l|g)\b"
        };
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) continue;
            productName = match.Groups["product"].Value.Trim(' ', '?', '.', ',');
            unit = match.Groups["unit"].Value;
            if (!string.IsNullOrWhiteSpace(productName)) return true;
        }
        productName = string.Empty;
        unit = string.Empty;
        return false;
    }
}
