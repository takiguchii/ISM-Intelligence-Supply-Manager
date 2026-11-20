namespace ISM.Domain.Modules.DataImport;

public sealed class ReceiptConfirmation
{
    public Guid ImportId { get; set; }
    public int RestaurantId { get; set; }
    public string ContentSha256 { get; set; } = string.Empty;
    public string DocumentIdentitySha256 { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestSha256 { get; set; } = string.Empty;
    public int ConfirmedByUserId { get; set; }
    public DateTime ConfirmedAtUtc { get; set; }
}
