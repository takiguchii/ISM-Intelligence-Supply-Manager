using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ISM.Application.DTOs.DataImport;

public sealed class ReceiptPreviewDto
{
    public string Sha256 { get; init; } = string.Empty;
    public string DocumentKind { get; init; } = "unknown";
    public string? Supplier { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? IssueDate { get; init; }
    public IReadOnlyList<ReceiptItemDto> Items { get; init; } = [];
    public decimal? Subtotal { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Total { get; init; }
    public decimal? Taxes { get; init; }
    public string? PaymentMethod { get; init; }
    public IReadOnlyList<string> UncertainFields { get; init; } = [];
    public IReadOnlyList<string> ValidationIssues { get; init; } = [];
    public string Decision { get; init; } = "MANUAL_REVIEW_REQUIRED";
    public bool RequiresConfirmation => true;
    public bool FiscalDocument => false;
}

public sealed record ReceiptItemDto(string Description, decimal? Quantity, string? Unit, decimal? UnitPrice, decimal? LineTotal);

public sealed class ReceiptVisionJson
{
    public string? DocumentKind { get; set; }
    public string? Supplier { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssueDate { get; set; }
    public List<ReceiptItemJson>? Items { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? Subtotal { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? Discount { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? Total { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? Taxes { get; set; }
    public string? PaymentMethod { get; set; }
    [JsonConverter(typeof(UncertainFieldsJsonConverter))]
    public List<string>? UncertainFields { get; set; }
}

public sealed class UncertainFieldsJsonConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new JsonException("uncertainFields deve ser uma lista.");

        var fields = new List<string>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            string? field = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Object when item.TryGetProperty("field", out var name) && name.ValueKind == JsonValueKind.String => name.GetString(),
                _ => throw new JsonException("Campo incerto inválido.")
            };
            if (string.IsNullOrWhiteSpace(field)) throw new JsonException("Nome do campo incerto vazio.");
            fields.Add(field.Trim());
        }
        return fields;
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var field in value) writer.WriteStringValue(field);
        writer.WriteEndArray();
    }
}

public sealed class ReceiptItemJson
{
    public string? Description { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? UnitPrice { get; set; }
    [JsonConverter(typeof(ReceiptDecimalJsonConverter))] public decimal? LineTotal { get; set; }
}

public sealed class ReceiptDecimalJsonConverter : JsonConverter<decimal?>
{
    private static readonly Regex NumberPattern = new(
        @"^(?:R\$\s*)?(?<number>[+-]?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:[,.]\d{1,3})?)(?:\s+(?:kg|g|l|ml|un|pct|cx))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.Number) return reader.GetDecimal();
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Valor decimal inválido.");

        var raw = reader.GetString()?.Trim() ?? string.Empty;
        if (raw.Length == 0 || raw.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;
        var match = NumberPattern.Match(raw);
        if (!match.Success) throw new JsonException("Formato decimal não reconhecido.");
        var number = match.Groups["number"].Value;
        if (number.Contains(',') && number.Contains('.')) number = number.Replace(".", string.Empty).Replace(',', '.');
        else number = number.Replace(',', '.');
        if (!decimal.TryParse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            throw new JsonException("Valor decimal fora do intervalo permitido.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}
