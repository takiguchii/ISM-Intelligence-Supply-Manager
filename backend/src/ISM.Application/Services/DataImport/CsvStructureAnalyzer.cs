using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Analisa a estrutura da planilha e separa os dados por categoria
/// (estoque, fornecedores, finanças...) usando os IImportCategoryProfile
/// registrados no DI. Gera a confirmação de cada categoria sem gravar no banco.
/// </summary>
public sealed class CsvStructureAnalyzer : ICsvStructureAnalyzer
{
    private readonly IEnumerable<IImportCategoryProfile> _profiles;

    public CsvStructureAnalyzer(IEnumerable<IImportCategoryProfile> profiles)
        => _profiles = profiles;

    public async Task<ImportPreviewDto> AnalyzeAsync(
        Stream fileContent,
        string fileName,
        string? contentType,
        CancellationToken ct)
    {
        fileContent.Position = 0;
        using var buffered = new MemoryStream();
        await fileContent.CopyToAsync(buffered, ct);
        buffered.Position = 0;

        var headers = await CsvParser.ReadHeadersAsync(buffered, ct);
        buffered.Position = 0;
        var (rows, parseErrors) = await CsvParser.ReadRowsAsync(buffered, ct);

        var selectedProfiles = _profiles
            .Select(p => (Profile: p, Score: p.MatchScore(headers)))
            .Where(x => x.Score >= IImportCategoryProfile.MinMatchThreshold)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Profile)
            .ToList();

        var usedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categories = new List<CategoryConfirmationDto>();
        foreach (var profile in selectedProfiles)
        {
            categories.Add(BuildConfirmation(profile, rows));
            foreach (var column in profile.GetConsumedColumns(headers))
                usedColumns.Add(column);
        }

        var unmapped = headers
            .Where(h => !usedColumns.Contains(h))
            .ToList();

        return new ImportPreviewDto
        {
            FileName = fileName,
            ContentType = contentType,
            TotalRows = rows.Count,
            Headers = headers,
            UnmappedColumns = unmapped,
            Categories = categories
        };
    }

    private static CategoryConfirmationDto BuildConfirmation(
        IImportCategoryProfile profile,
        List<Dictionary<string, string>> rows)
    {
        var mappedFields = new List<Dictionary<string, string?>>();
        var rowsWithErrors = new List<int>();
        var errors = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var mapped = profile.MapRow(row);

            var error = profile.Validate(mapped);
            if (error is null)
            {
                mappedFields.Add(mapped);
            }
            else
            {
                // linha do arquivo (1-based, header é a linha 1 — mesmo padrão dos importers)
                var sourceRow = i + 2;
                rowsWithErrors.Add(sourceRow);
                errors.Add($"Linha {sourceRow}: {error}");
            }
        }

        return new CategoryConfirmationDto
        {
            Category = profile.Category,
            TargetEntity = profile.TargetEntity,
            RowCount = mappedFields.Count + rowsWithErrors.Count,
            MappedFields = mappedFields,
            RowsWithErrors = rowsWithErrors,
            Errors = errors
        };
    }
}
