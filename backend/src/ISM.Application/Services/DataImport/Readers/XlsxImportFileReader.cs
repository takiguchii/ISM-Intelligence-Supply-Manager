using ClosedXML.Excel;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>Reader de planilhas .xlsx/.xlsm (Open XML) via ClosedXML.</summary>
public sealed class XlsxImportFileReader : IImportFileReader
{
    private static readonly string[] Extensions = [".xlsx", ".xlsm"];

    public bool CanHandle(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName ?? "");
        return Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase)
            || string.Equals(contentType,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                StringComparison.OrdinalIgnoreCase);
    }

    public Task<ImportFileContent> ReadAsync(Stream fileContent, CancellationToken ct)
    {
        var errors = new List<ImportErrorLog>();
        try
        {
            using var workbook = new XLWorkbook(fileContent);
            var worksheet = workbook.Worksheets.Worksheet(1);

            var usedRange = worksheet.RangeUsed();
            if (usedRange is null)
                return Task.FromResult(new ImportFileContent([], [], errors));

            var rows = new List<Dictionary<string, string>>();
            List<string> headers = [];
            var first = true;

            foreach (var row in usedRange.Rows())
            {
                var values = row.Cells().Select(c => c.GetString().Trim()).ToList();

                if (first)
                {
                    // Cabeçalhos vazios recebem nome genérico para não colidirem no dicionário
                    for (var i = 0; i < values.Count; i++)
                        headers.Add(string.IsNullOrWhiteSpace(values[i]) ? $"Coluna{i + 1}" : values[i]);
                    first = false;
                    continue;
                }

                if (values.All(string.IsNullOrWhiteSpace)) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < headers.Count; i++)
                    dict[headers[i]] = i < values.Count ? values[i] : string.Empty;
                rows.Add(dict);
            }

            return Task.FromResult(new ImportFileContent(headers, rows, errors));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Não foi possível ler a planilha Excel. Verifique se o arquivo .xlsx está íntegro.", ex);
        }
    }
}
