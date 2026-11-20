using System.Text.Json;
using ISM.Application.Agents;
using ISM.Application.DTOs.Assistant;

namespace ISM.Application.Services.Assistant;

public sealed class AssistantIntentCoordinator(IAgentIntentRouter router)
{
    public AgentIntent? Resolve(string message, AssistantContextResponse? context)
    {
        var normalized = message.Trim().ToLowerInvariant();
        if (context is not null && context.ReferenceEntityId is int entityId)
        {
            if (context.ReferenceEntityType == "Product" && (normalized.Contains("repor") || normalized.Contains("comprar") || normalized.Contains("ruptura")))
                return Intent("stock", "stock.analyzeReplenishment", new { productId = entityId });

            if (context.ReferenceEntityType == "Dish" && (normalized.Contains("preço") || normalized.Contains("preco") || normalized.Contains("cmv") || normalized.Contains("margem")))
                return Intent("pricing", "pricing.simulateDishPrice", new { dishId = entityId });

            if (context.ReferenceEntityType == "Product" && (normalized.Contains("fornecedor") || normalized.Contains("cotação") || normalized.Contains("cotacao")))
            {
                context.Facts.TryGetValue("unidade", out var unit);
                return Intent("suppliers", "suppliers.compareOffers", new { productName = context.EntityName, unit = unit ?? string.Empty });
            }

            if (normalized.Contains("alerta") || normalized.Contains("detalhe"))
                return Intent("assistant", "assistant.getAlertContext", new { alertId = context.AlertId });
        }

        return router.Resolve(message);
    }

    private static AgentIntent Intent(string specialist, string toolName, object input)
        => new(specialist, toolName, JsonSerializer.SerializeToElement(input), 1d);
}
