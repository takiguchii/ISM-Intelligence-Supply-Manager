using ISM.Application.DTOs.DataImport;

namespace ISM.Application.Interfaces.DataImport;

public interface ICsvStructureAnalyzer
{
    /// <summary>
    /// Analisa a planilha sem gravar nada: detecta estrutura, separa os dados
    /// por categoria e gera as confirmações (Estoque, Fornecedores, Finanças, ...).
    /// </summary>
    Task<ImportPreviewDto> AnalyzeAsync(
        Stream fileContent,
        string fileName,
        string? contentType,
        CancellationToken ct);
}
