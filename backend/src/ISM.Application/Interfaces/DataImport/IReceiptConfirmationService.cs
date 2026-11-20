using ISM.Application.DTOs.DataImport;

namespace ISM.Application.Interfaces.DataImport;

public interface IReceiptConfirmationService
{
    Task<ReceiptConfirmationResultDto> ConfirmAsync(byte[] imageBytes, string fileName, string contentType,
        ReceiptConfirmationRequestDto request, CancellationToken ct);
}
