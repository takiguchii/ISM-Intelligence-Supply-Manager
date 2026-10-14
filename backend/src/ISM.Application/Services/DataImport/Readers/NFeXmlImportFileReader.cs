using System.Globalization;
using System.Xml.Linq;
using ISM.Application.Common;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>
/// Reader de XML NF-e (padrão SEFAZ): extrai os itens da nota como linhas
/// de estoque (produto, unidade, quantidade, custo unitário).
/// Também extrai metadados do emitente e data da compra para histórico
/// de preços de fornecedores (disponível em Metadata e ExtendedRows).
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

            var infNFe = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe");
            if (infNFe is null)
                throw new InvalidOperationException(
                    "O XML não contém a tag 'infNFe'. Confirme que é uma NF-e válida.");

            var nfeId = infNFe.Attribute("Id")?.Value.Trim() ?? string.Empty;

            var emit = infNFe.Descendants().FirstOrDefault(e => e.Name.LocalName == "emit");
            string? EmitVal(string name) =>
                emit?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim();

            var supplierRawName = EmitVal("xNome") ?? string.Empty;
            var supplierCnpj = EmitVal("CNPJ")
                               ?? EmitVal("CPF")
                               ?? string.Empty;

            DateTime purchasedAtUtc;
            var dhEmiStr = infNFe.Descendants().FirstOrDefault(e => e.Name.LocalName == "dhEmi")?.Value;
            if (!string.IsNullOrWhiteSpace(dhEmiStr) &&
                DateTime.TryParse(dhEmiStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dhEmi))
            {
                purchasedAtUtc = dhEmi.ToUniversalTime();
            }
            else
            {
                var dEmiStr = infNFe.Descendants().FirstOrDefault(e => e.Name.LocalName == "dEmi")?.Value;
                var hEmiStr = infNFe.Descendants().FirstOrDefault(e => e.Name.LocalName == "hEmi")?.Value;
                var datePart = !string.IsNullOrWhiteSpace(dEmiStr)
                    ? DateTime.TryParseExact(dEmiStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                        ? d
                        : DateTime.TryParse(dEmiStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                            ? d
                            : DateTime.UtcNow.Date
                    : DateTime.UtcNow.Date;
                if (!string.IsNullOrWhiteSpace(hEmiStr) &&
                    TimeSpan.TryParse(hEmiStr, CultureInfo.InvariantCulture, out var ts))
                {
                    datePart = datePart.Add(ts);
                }
                purchasedAtUtc = DateTime.SpecifyKind(datePart, DateTimeKind.Utc);
            }

            // Navegação por LocalName para ignorar diferenças de namespace entre versões do layout
            var detItems = infNFe.Descendants().Where(e => e.Name.LocalName == "det").ToList();

            if (detItems.Count == 0)
                throw new InvalidOperationException(
                    "O XML não contém itens de produto (elementos 'det'). Confirme que é uma NF-e válida.");

            const string Headers = "Nome|Unidade|QuantidadeAtual|CustoMedio";
            var headerList = Headers.Split('|');
            var rows = new List<Dictionary<string, string>>();
            var extendedRows = new List<Dictionary<string, object?>>();

            foreach (var det in detItems)
            {
                var prod = det.Elements().FirstOrDefault(e => e.Name.LocalName == "prod");
                if (prod is null) continue;

                string Val(string name) =>
                    prod.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim() ?? string.Empty;

                var xProd = Val("xProd");
                var uCom = Val("uCom").ToLowerInvariant();
                var qCom = Val("qCom");
                var vUnCom = Val("vUnCom");

                static decimal ParseDecimal(string s, decimal fallback = 0m)
                {
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out d)) return d;
                    return fallback;
                }

                rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Nome"] = xProd,
                    ["Unidade"] = uCom,
                    ["QuantidadeAtual"] = qCom,
                    ["CustoMedio"] = vUnCom,
                });

                extendedRows.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["supplier_raw_name"] = supplierRawName,
                    ["supplier_cnpj"] = supplierCnpj,
                    ["product_raw_name"] = xProd,
                    ["product_normalized"] = StringHelper.NormalizeName(xProd),
                    ["unit"] = uCom,
                    ["unit_price"] = ParseDecimal(vUnCom),
                    ["quantity_purchased"] = ParseDecimal(qCom),
                    ["purchased_at_utc"] = purchasedAtUtc,
                    ["nfe_access_key_or_import_id"] = nfeId,
                });
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["nfe.access_key"] = nfeId,
                ["nfe.supplier_raw_name"] = supplierRawName,
                ["nfe.supplier_raw_name_normalized"] = StringHelper.NormalizeName(supplierRawName),
                ["nfe.supplier_cnpj"] = supplierCnpj,
                ["nfe.purchased_at_utc"] = purchasedAtUtc.ToString("O"),
                ["nfe.items_count"] = extendedRows.Count.ToString(CultureInfo.InvariantCulture),
                ["origin"] = "nfe_xml",
            };

            return Task.FromResult(new ImportFileContent(
                headerList,
                rows,
                errors,
                metadata,
                extendedRows));
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

