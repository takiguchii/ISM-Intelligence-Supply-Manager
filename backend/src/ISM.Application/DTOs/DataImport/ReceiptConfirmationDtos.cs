namespace ISM.Application.DTOs.DataImport;

public sealed class ReceiptConfirmationRequestDto
{
    public bool Confirm { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ExpectedSha256 { get; set; } = string.Empty;
    public IReadOnlyList<ReceiptProductMappingDto> ProductMappings { get; set; } = [];
}

public sealed class ReceiptProductMappingDto
{
    public int LineIndex { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityInStockUnit { get; set; }
    public string? ConversionNote { get; set; }
}

public sealed record ReceiptConfirmationResultDto(Guid ImportId, bool AlreadyConfirmed, int ProductsUpdated, string Sha256);
