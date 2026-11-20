namespace ISM.Application.Interfaces.DataImport;

public interface IReceiptVisionExtractor
{
    Task<string> ExtractReceiptJsonAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct);
}
