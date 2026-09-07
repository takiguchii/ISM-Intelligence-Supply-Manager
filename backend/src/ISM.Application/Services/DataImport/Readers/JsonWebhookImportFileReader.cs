using System.Globalization;
using System.Text.Json;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>
/// Reader de payloads JSON de vendas/pedidos (webhook de ERP, PDV, iFood...).
/// Cada pedido pago vira uma linha no formato financeiro. Pedidos cancelados
/// são ignorados (não contabilizam receita).
/// </summary>
public sealed class JsonWebhookImportFileReader : IImportFileReader
{
    private static readonly string[] Extensions = [".json"];

    public bool CanHandle(string fileName, string? contentType)
        => Extensions.Contains(Path.GetExtension(fileName ?? ""), StringComparer.OrdinalIgnoreCase)
           || string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase);

    /// <summary>True se o conteúdo parece JSON (usado pelo resolver).</summary>
    public static bool MatchesContent(string headText)
    {
        var t = headText.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return t.StartsWith('{') || t.StartsWith('[');
    }

    public Task<ImportFileContent> ReadAsync(Stream fileContent, CancellationToken ct)
    {
        var errors = new List<ImportErrorLog>();
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(fileContent);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON inválido: {ex.Message}", ex);
        }

        using var _ = doc;

        // Aceita { "pedidos": [...] } ou um array direto
        var orders = doc.RootElement.ValueKind switch
        {
            JsonValueKind.Array => doc.RootElement.Clone(),
            JsonValueKind.Object when doc.RootElement.TryGetProperty("pedidos", out var p)
                && p.ValueKind == JsonValueKind.Array => p.Clone(),
            _ => default
        };

        if (orders.ValueKind != JsonValueKind.Array || orders.GetArrayLength() == 0)
            throw new InvalidOperationException(
                "JSON não contém pedidos. Esperado um array ou objeto com a propriedade 'pedidos'.");

        const string Headers = "Data|Historico|Tipo|Valor|FormaPagamento";
        var headerList = Headers.Split('|');
        var rows = new List<Dictionary<string, string>>();

        foreach (var order in orders.EnumerateArray())
        {
            var status = GetStr(order, "status");
            if (status is "Canceled" or "Cancelled" or "Cancelado" or "canceled")
                continue; // cancelados não contabilizam

            decimal total = 0m;
            if (order.TryGetProperty("itens", out var itens) && itens.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itens.EnumerateArray())
                {
                    var qty = GetDec(item, "quantidade") ?? 0m;
                    var unit = GetDec(item, "valorUnitario") ?? 0m;
                    total += qty * unit;
                }
            }
            total += GetDec(order, "taxaEntrega") ?? 0m;

            var dateRaw = GetStr(order, "orderedAtUtc");
            var date = DateTime.TryParse(dateRaw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;

            var canal = GetStr(order, "canal") ?? "pdv";
            var externalId = GetStr(order, "externalOrderId") ?? "—";

            rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Data"] = date,
                ["Historico"] = $"{canal}/{externalId}",
                ["Tipo"] = string.IsNullOrWhiteSpace(status) ? "receita" : status.ToLowerInvariant(),
                ["Valor"] = total.ToString("0.00##", CultureInfo.InvariantCulture),
                ["FormaPagamento"] = GetStr(order, "formaPagamento") ?? string.Empty,
            });
        }

        return Task.FromResult(new ImportFileContent(headerList, rows, errors));
    }

    private static string? GetStr(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()?.Trim()
            : null;

    private static decimal? GetDec(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number when v.TryGetDecimal(out var d) => d,
            JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Any,
                CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }
}
