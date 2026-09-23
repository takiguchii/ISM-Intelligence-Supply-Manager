namespace ISM.Application.Interfaces.DataImport;

/// <summary>
/// Extrai uma planilha em CSV a partir de mídia não estruturada
/// (foto de caderno, PDF escaneado, print de tabela).
/// A saída é um CSV virtual que entra no mesmo pipeline do CsvStructureAnalyzer.
/// </summary>
public interface IPhotoImportExtractor
{
    /// <summary>Retorna o conteúdo extraído como CSV (primeira linha = cabeçalho).</summary>
    Task<string> ExtractCsvAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        CancellationToken ct);
}
