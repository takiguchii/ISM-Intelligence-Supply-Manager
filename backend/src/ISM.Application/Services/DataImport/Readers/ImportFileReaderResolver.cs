using System.Text;
using ISM.Application.Interfaces.DataImport;

namespace ISM.Application.Services.DataImport.Readers;

/// <summary>
/// Detecta o formato do arquivo (extensão + assinatura do conteúdo) e delega
/// para o reader adequado. Qualquer formato desconhecido cai no leitor CSV
/// (comportamento anterior), mantendo compatibilidade.
/// </summary>
public sealed class ImportFileReaderResolver : IImportFileReaderResolver
{
    private readonly IEnumerable<IImportFileReader> _readers;
    private readonly CsvImportFileReader _csvFallback;

    public ImportFileReaderResolver(IEnumerable<IImportFileReader> readers)
    {
        _readers = readers;
        _csvFallback = readers.OfType<CsvImportFileReader>().FirstOrDefault() ?? new CsvImportFileReader();
    }

    public async Task<ImportFileContent> ReadAsync(
        Stream fileContent, string fileName, string? contentType, CancellationToken ct)
    {
        fileContent.Position = 0;
        using var buffered = new MemoryStream();
        await fileContent.CopyToAsync(buffered, ct);
        var bytes = buffered.ToArray();

        var headText = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
        var isZip = bytes.Length > 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04;

        var reader = Pick(fileName, contentType, headText, isZip);

        buffered.Position = 0;
        return await reader.ReadAsync(buffered, ct);
    }

    private IImportFileReader Pick(string fileName, string? contentType, string headText, bool isZip)
    {
        // 1) XLSX: extensão/content-type OU assinatura ZIP (que não é CSV/XML/JSON)
        if (_readers.FirstOrDefault(r => r is not CsvImportFileReader && r.CanHandle(fileName, contentType))
                is IImportFileReader directMatch
            && !(directMatch is NFeXmlImportFileReader || directMatch is SpreadSheetMlImportFileReader
                 || directMatch is JsonWebhookImportFileReader))
            return directMatch;

        if (isZip)
            return Require(XlsxKind);

        // 2) XML: desambiguar entre NF-e e SpreadsheetML pelo conteúdo
        if (headText.TrimStart('\uFEFF', ' ', '\t', '\r', '\n').StartsWith('<'))
        {
            if (NFeXmlImportFileReader.MatchesContent(headText)) return Require(NfeKind);
            if (SpreadSheetMlImportFileReader.MatchesContent(headText)) return Require(SmlKind);
            throw new InvalidOperationException(
                "O arquivo é um XML, mas de um tipo não suportado (esperado NF-e ou planilha SpreadsheetML).");
        }

        // 3) JSON
        if (JsonWebhookImportFileReader.MatchesContent(headText))
            return Require(JsonKind);

        // 4) Fallback: CSV (inclusive formatos desconhecidos)
        return _csvFallback;
    }

    private const string XlsxKind = nameof(XlsxKind);
    private const string NfeKind = nameof(NfeKind);
    private const string SmlKind = nameof(SmlKind);
    private const string JsonKind = nameof(JsonKind);

    private IImportFileReader Require(string kind) => kind switch
    {
        nameof(XlsxKind) => OfType<XlsxImportFileReader>(),
        nameof(NfeKind) => OfType<NFeXmlImportFileReader>(),
        nameof(SmlKind) => OfType<SpreadSheetMlImportFileReader>(),
        nameof(JsonKind) => OfType<JsonWebhookImportFileReader>(),
        _ => throw new InvalidOperationException($"Formato '{kind}' sem reader registrado.")
    };

    private IImportFileReader OfType<T>() where T : IImportFileReader
        => _readers.OfType<T>().FirstOrDefault()
           ?? (IImportFileReader)(T)Activator.CreateInstance(typeof(T))!;
}
