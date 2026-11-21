using System.Security.Cryptography;
using System.Text.Json;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;

namespace ISM.Application.Services.DataImport;

public sealed class ReceiptPreviewService(IReceiptVisionExtractor extractor)
{
    private const int MaxImageBytes = 12 * 1024 * 1024;

    public async Task<ReceiptPreviewDto> AnalyzeAsync(byte[] bytes, string fileName, string contentType, CancellationToken ct)
    {
        if (bytes.Length == 0 || bytes.Length > MaxImageBytes)
            throw new InvalidOperationException("Imagem vazia ou acima do limite de 12 MB.");
        if (!IsSupportedImageOrPdf(bytes, contentType))
            throw new InvalidOperationException("Envie uma imagem PNG, JPEG, WEBP ou PDF válido.");

        var json = await extractor.ExtractReceiptJsonAsync(bytes, Path.GetFileName(fileName), contentType, ct);
        ReceiptVisionJson source;
        try
        {
            source = JsonSerializer.Deserialize<ReceiptVisionJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Objeto vazio.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Não foi possível validar a resposta estruturada do modelo.");
        }

        var items = (source.Items ?? []).Select(x => new ReceiptItemDto(x.Description?.Trim() ?? string.Empty, x.Quantity, x.Unit?.Trim(), x.UnitPrice, x.LineTotal)).ToArray();
        var issues = new List<string>();
        var supplier = NormalizeUnknown(source.Supplier);
        var documentNumber = NormalizeUnknown(source.DocumentNumber);
        if (supplier is null) issues.Add("Fornecedor ausente ou ilegível.");
        if (documentNumber is null) issues.Add("Número do documento ausente ou ilegível.");
        DateOnly? issueDate = DateOnly.TryParse(source.IssueDate, out var parsedDate) ? parsedDate : null;
        var isPurchase = source.DocumentKind?.Trim().ToLowerInvariant() is "purchase" or "compra" or "compra_entrada";
        if (items.Length == 0) issues.Add("Documento sem itens reconhecíveis.");
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (string.IsNullOrWhiteSpace(item.Description) || item.Quantity is null or <= 0 || string.IsNullOrWhiteSpace(item.Unit) || item.UnitPrice is null or < 0 || item.LineTotal is null or < 0)
                issues.Add($"Item {i + 1} tem campo obrigatório ausente ou inválido.");
            else if (Math.Abs(item.Quantity.Value * item.UnitPrice.Value - item.LineTotal.Value) > 0.02m)
                issues.Add($"Item {i + 1}: quantidade × preço unitário diverge do total da linha.");
        }
        var sumLines = items.Where(x => x.LineTotal.HasValue).Sum(x => x.LineTotal!.Value);
        if (source.Subtotal is null || Math.Abs(sumLines - source.Subtotal.Value) > 0.02m)
            issues.Add("Subtotal ausente ou diferente da soma das linhas.");
        var discount = source.Discount ?? (source.Subtotal.HasValue && source.Total.HasValue && source.Subtotal.Value == source.Total.Value ? 0m : (decimal?)null);
        if (source.Total is null || source.Subtotal is null || discount is null || Math.Abs(source.Subtotal.Value - discount.Value - source.Total.Value) > 0.02m)
            issues.Add("Total ausente ou incompatível com subtotal menos desconto.");
        if (!string.IsNullOrWhiteSpace(source.IssueDate) && issueDate is null) issues.Add("Data do documento ilegível ou inválida.");

        var uncertain = source.UncertainFields ?? [];
        var decision = !isPurchase || items.Length == 0
            ? "BLOCK_NO_STOCK_ENTRY"
            : uncertain.Count > 0 || issues.Count > 0 || issueDate is null
                ? "MANUAL_REVIEW_REQUIRED"
                : "PREVIEW_REQUIRES_CONFIRMATION";

        return new ReceiptPreviewDto
        {
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            DocumentKind = isPurchase ? "purchase" : source.DocumentKind ?? "unknown",
            Supplier = supplier,
            DocumentNumber = documentNumber,
            IssueDate = issueDate,
            Items = items,
            Subtotal = source.Subtotal,
            Discount = discount,
            Total = source.Total,
            Taxes = source.Taxes,
            PaymentMethod = source.PaymentMethod?.Trim(),
            UncertainFields = uncertain,
            ValidationIssues = issues,
            Decision = decision
        };
    }

    private static string? NormalizeUnknown(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || trimmed.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("desconhecido", StringComparison.OrdinalIgnoreCase)
            ? null : trimmed;
    }

    private static bool IsSupportedImageOrPdf(byte[] bytes, string contentType)
    {
        var normalized = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return normalized switch
        {
            "application/pdf" => bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
