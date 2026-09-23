using ISM.Application.Interfaces.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>Reader CSV (também é o fallback quando o formato não é reconhecido).</summary>
public sealed class CsvImportFileReader : IImportFileReader
{
    public bool CanHandle(string fileName, string? contentType) => CsvParser.IsCsv(contentType, fileName);

    public async Task<ImportFileContent> ReadAsync(Stream fileContent, CancellationToken ct)
    {
        var (rows, errors) = await CsvParser.ReadRowsAsync(fileContent, ct);
        var headers = rows.Count > 0
            ? rows[0].Keys.ToList()
            : await CsvParser.ReadHeadersAsync(fileContent.CanSeek ? Rewind(fileContent) : fileContent, ct);
        return new ImportFileContent(headers, rows, errors);
    }

    private static Stream Rewind(Stream s)
    {
        s.Position = 0;
        return s;
    }
}
