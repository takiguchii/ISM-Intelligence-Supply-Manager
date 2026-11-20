namespace ISM.Application.Services.DataImport;

public sealed class ReceiptConflictException(string message) : Exception(message);
