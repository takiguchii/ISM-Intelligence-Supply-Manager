using System.Text;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Fluxo de importação por foto/PDF escaneado: extrai a tabela via
/// IPhotoImportExtractor (visão), converte em CSV virtual e reutiliza o
/// CsvStructureAnalyzer — mesma separação por categoria e confirmação do CSV.
/// </summary>
public sealed class PhotoImportService : IPhotoImportService
{
    private readonly IPhotoImportExtractor _extractor;
    private readonly ICsvStructureAnalyzer _analyzer;

    public PhotoImportService(IPhotoImportExtractor extractor, ICsvStructureAnalyzer analyzer)
    {
        _extractor = extractor;
        _analyzer = analyzer;
    }

    public async Task<ImportPreviewDto> AnalyzePhotoAsync(
        Stream fileContent,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        using var buffered = new MemoryStream();
        await fileContent.CopyToAsync(buffered, ct);
        var bytes = buffered.ToArray();

        // 1. Mídia não estruturada → texto tabular (CSV virtual)
        var csvText = await _extractor.ExtractCsvAsync(bytes, fileName, contentType, ct);

        // 2. CSV virtual → mesmo pipeline de análise/confirmação do upload de planilha
        await using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvText));
        return await _analyzer.AnalyzeAsync(csvStream, fileName, contentType, ct);
    }
}
