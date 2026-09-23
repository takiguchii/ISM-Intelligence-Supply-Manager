using System.Xml.Linq;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>
/// Reader de XML NF-e (padrão SEFAZ): extrai os itens da nota como linhas
/// de estoque (produto, unidade, quantidade, custo unitário).
/// Limitação v1: o emitente (fornecedor) ainda não é importado automaticamente.
/// </summary>
public sealed class NFeXmlImportFileReader : IImportFileReader
{
    public bool CanHandle(string fileName, string? contentType)
        => Path.GetExtension(fileName ?? "").Equals(".xml", StringComparison.OrdinalIgnoreCase)
           || string.Equals(contentType, "application/xml", StringComparison.OrdinalIgnoreCase)
           || string.Equals(contentType, "text/xml", StringComparison.OrdinalIgnoreCase);

    /// <summary>True se o conteúdo parece ser uma NF-e (usado pelo resolver para desambiguar XMLs).</summary>
    public static bool MatchesContent(string headText)
        => headText.Contains("infNFe", StringComparison.OrdinalIgnoreCase)
           || headText.Contains("<NFe", StringComparison.OrdinalIgnoreCase);

    public Task<ImportFileContent> ReadAsync(Stream fileContent, CancellationToken ct)
    {
        var errors = new List<ImportErrorLog>();
        try
        {
            var doc = XDocument.Load(fileContent);

            // Navegação por LocalName para ignorar diferenças de namespace entre versões do layout
            var detItems = doc.Descendants().Where(e => e.Name.LocalName == "det").ToList();

            if (detItems.Count == 0)
                throw new InvalidOperationException(
                    "O XML não contém itens de produto (elementos 'det'). Confirme que é uma NF-e válida.");

            const string Headers = "Nome|Unidade|QuantidadeAtual|CustoMedio";
            var headerList = Headers.Split('|');
            var rows = new List<Dictionary<string, string>>();

            foreach (var det in detItems)
            {
                var prod = det.Elements().FirstOrDefault(e => e.Name.LocalName == "prod");
                if (prod is null) continue;

                string Val(string name) =>
                    prod.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim() ?? string.Empty;

                rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Nome"] = Val("xProd"),
                    ["Unidade"] = Val("uCom").ToLowerInvariant(),
                    ["QuantidadeAtual"] = Val("qCom"),
                    ["CustoMedio"] = Val("vUnCom"),
                });
            }

            return Task.FromResult(new ImportFileContent(headerList, rows, errors));
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível ler o XML como NF-e: {ex.Message}", ex);
        }
    }
}
