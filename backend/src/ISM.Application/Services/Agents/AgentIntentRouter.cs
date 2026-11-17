using System.Text.Json;
using ISM.Application.Agents;

namespace ISM.Application.Services.Agents;

public sealed class AgentIntentRouter : IAgentIntentRouter
{
    private static readonly IReadOnlyDictionary<string, (string Specialist, string[] Arguments)> ToolSchema
        = new Dictionary<string, (string, string[])>(StringComparer.Ordinal)
        {
            ["finance.getRevenueSummary"] = ("finance", Array.Empty<string>()),
            ["finance.getPeriodSummary"] = ("finance", ["startUtc", "endUtc"]),
            ["stock.getProduct"] = ("stock", ["productId"]),
            ["stock.analyzeReplenishment"] = ("stock", ["productId"]),
            ["pricing.getDishCost"] = ("pricing", ["dishId"]),
            ["pricing.simulateDishPrice"] = ("pricing", ["dishId"]),
            ["suppliers.compareOffers"] = ("suppliers", ["productName", "unit"]),
            ["assistant.getAlertContext"] = ("assistant", ["alertId"])
        };

    public AgentIntent? Resolve(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        var normalized = message.Trim().ToLowerInvariant();
        if (normalized.Contains("faturamento") || normalized.Contains("receita") || normalized.Contains("despesa") || normalized.Contains("fluxo de caixa"))
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
}
