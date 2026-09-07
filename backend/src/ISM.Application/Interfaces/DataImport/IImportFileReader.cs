using ISM.Application.DTOs.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Interfaces.DataImport;

/// <summary>
/// Conteúdo normalizado de um arquivo importado: qualquer formato
/// (CSV, XLSX, XML NF-e, SpreadsheetML, JSON) vira esta mesma estrutura,
/// alimentando o analyzer e os importers sem duplicar lógica.
/// </summary>
public sealed record ImportFileContent(
    IReadOnlyList<string> Headers,
    IReadOnlyList<Dictionary<string, string>> Rows,
    IReadOnlyList<ImportErrorLog> Errors);

public interface IImportFileReader
{
    bool CanHandle(string fileName, string? contentType);

    Task<ImportFileContent> ReadAsync(Stream fileContent, CancellationToken ct);
}

/// <summary>
/// Escolhe o reader adequado ao formato (sniffing por extensão + assinatura
/// do conteúdo) e devolve as linhas normalizadas. Fallback: CSV.
/// </summary>
public interface IImportFileReaderResolver
{
    Task<ImportFileContent> ReadAsync(Stream fileContent, string fileName, string? contentType, CancellationToken ct);
}
