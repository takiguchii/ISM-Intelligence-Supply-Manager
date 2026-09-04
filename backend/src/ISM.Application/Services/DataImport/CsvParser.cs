using System.Text;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Parser CSV compartilhado por todos os importers e pelo analisador de estrutura.
/// </summary>
public static class CsvParser
{
    public static bool IsCsv(string? contentType, string? fileName)
    {
        return
            string.Equals(contentType, "text/csv", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contentType, "application/csv", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contentType, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase) ||
            (fileName ?? string.Empty).EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Lê o CSV inteiro como lista de dicionários coluna → valor.</summary>
    public static async Task<(List<Dictionary<string, string>> rows, List<ImportErrorLog> errors)>
        ReadRowsAsync(Stream stream, CancellationToken ct)
    {
        var rows = new List<Dictionary<string, string>>();
        var errors = new List<ImportErrorLog>();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var headerLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(headerLine))
            return (rows, errors);

        var columns = ParseLine(headerLine);
        if (columns.Count == 0)
            return (rows, errors);

        var rowNumber = 2;
        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                rowNumber++;
                continue;
            }
            try
            {
                var values = ParseLine(line);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                    dict[columns[i]] = i < values.Count ? values[i] : string.Empty;
                rows.Add(dict);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorLog
                {
                    SourceRowNumber = rowNumber,
                    ErrorMessage = $"Falha ao parsear linha: {ex.Message}",
                    RawRowPayloadJson = line,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            rowNumber++;
        }
        return (rows, errors);
    }

    public static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else current.Append(c);
            }
            else
            {
                if (c == ',')
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else if (c == '"') inQuotes = true;
                else current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return result;
    }

    /// <summary>Retorna os cabeçalhos da planilha sem ler as linhas de dados.</summary>
    public static async Task<List<string>> ReadHeadersAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var headerLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(headerLine) ? [] : ParseLine(headerLine);
    }
}
