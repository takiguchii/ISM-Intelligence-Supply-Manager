using ISM.Application.DTOs.DataImport;

namespace ISM.Application.Interfaces.DataImport;

public interface IPhotoImportService
{
    /// <summary>
    /// Extrai a tabela de uma imagem/PDF escaneado via visão computacional,
    /// converte em CSV virtual e gera as confirmações por categoria (dry-run).
    /// </summary>
    Task<ImportPreviewDto> AnalyzePhotoAsync(
        Stream fileContent,
        string fileName,
        string contentType,
        CancellationToken ct);
}
