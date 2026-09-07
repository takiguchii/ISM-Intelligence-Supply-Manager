using System.Xml.Linq;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>
/// Reader de planilhas em formato SpreadsheetML 2003 (XML legado que o Excel
/// abre/exporta — namespace urn:schemas-microsoft-com:office:spreadsheet).
/// </summary>
public sealed class SpreadSheetMlImportFileReader : IImportFileReader
{
    private static readonly XNamespace Ss = "urn:schemas-microsoft-com:office:spreadsheet";

    public bool CanHandle(string fileName, string? contentType)
        => Path.GetExtension(fileName ?? "").Equals(".xml", StringComparison.OrdinalIgnoreCase);

    /// <summary>True se o conteúdo é SpreadsheetML (usado pelo resolver para desambiguar XMLs).</summary>
    public static bool MatchesContent(string headText)
        => headText.Contains("urn:schemas-microsoft-com:office:spreadsheet", StringComparison.OrdinalIgnoreCase)
           || headText.Contains("Workbook", StringComparison.OrdinalIgnoreCase);

    public Task<ImportFileContent> ReadAsync(Stream fileContent, CancellationToken ct)
    {
        var errors = new List<ImportErrorLog>();
        try
        {
            var doc = XDocument.Load(fileContent);
            var table = doc
                .Descendants(Ss + "Worksheet")
                .FirstOrDefault()?
                .Element(Ss + "Table");

            if (table is null)
                throw new InvalidOperationException(
                    "O XML não contém uma planilha (Worksheet/Table) reconhecível.");

            var rows = new List<Dictionary<string, string>>();
            List<string> headers = [];
            var first = true;

            foreach (var row in table.Elements(Ss + "Row"))
            {
                // Cells podem ser esparsas (ss:Index pula colunas) — preencher lacunas com vazio
                var values = new List<string>();
                var expectedIndex = 1;

                foreach (var cell in row.Elements(Ss + "Cell"))
                {
                    var indexAttr = cell.Attribute(Ss + "Index");
                    var index = indexAttr != null ? int.Parse(indexAttr.Value) : expectedIndex;

                    while (expectedIndex < index)
                    {
                        values.Add(string.Empty);
                        expectedIndex++;
                    }

                    var data = cell.Element(Ss + "Data")?.Value.Trim() ?? string.Empty;
                    values.Add(data);
                    expectedIndex++;
                }

                if (first)
                {
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
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível ler a planilha XML (SpreadsheetML): {ex.Message}", ex);
        }
    }
}
