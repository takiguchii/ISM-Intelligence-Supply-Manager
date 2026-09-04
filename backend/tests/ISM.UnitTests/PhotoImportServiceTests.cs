using System.Text;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.Services.DataImport;

namespace ISM.UnitTests;

/// <summary>Extractor fake que devolve CSV fixo — simula o LLM de visão.</summary>
internal sealed class FakePhotoImportExtractor(string csv) : IPhotoImportExtractor
{
    public string? ReceivedFileName { get; private set; }
    public Task<string> ExtractCsvAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct)
    {
        ReceivedFileName = fileName;
        return Task.FromResult(csv);
    }
}

public class PhotoImportServiceTests
{
    private static PhotoImportService CreateService(FakePhotoImportExtractor extractor) => new(
        extractor,
        new CsvStructureAnalyzer(
        [
            new StockCategoryProfile(),
            new SuppliersCategoryProfile(),
            new FinanceCategoryProfile()
        ],
        new ImportFileReaderResolver([new CsvImportFileReader()])));

    private static Stream Bytes(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task FotoDeEstoque_DeveGerarConfirmacaoDeEstoque()
    {
        var extractor = new FakePhotoImportExtractor(
            "Nome,Unidade,Quantidade Atual,Custo\nArroz,kg,50,5.90\nFeijão,L,20,\n");
        var service = CreateService(extractor);

        var preview = await service.AnalyzePhotoAsync(
            Bytes("fake-image-bytes"), "foto-caderno.jpg", "image/jpeg", CancellationToken.None);

        Assert.Equal("foto-caderno.jpg", preview.FileName);
        var estoque = Assert.Single(preview.Categories);
        Assert.Equal("estoque", estoque.Category);
        Assert.True(estoque.CanImport);
        Assert.Equal("Arroz", estoque.MappedFields[0]["nome"]);
    }

    [Fact]
    public async Task FotoComLinhaIlegivel_DeveReportarErroNaConfirmacao()
    {
        var extractor = new FakePhotoImportExtractor(
            "Nome,QuantidadeAtual\n,10\n"); // nome ilegível → vazio
        var service = CreateService(extractor);

        var preview = await service.AnalyzePhotoAsync(Bytes("img"), "foto.jpg", "image/jpeg", CancellationToken.None);

        var estoque = Assert.Single(preview.Categories);
        Assert.False(estoque.CanImport);
        Assert.Contains(2, estoque.RowsWithErrors);
    }

    [Fact]
    public async Task FotoSemTabelaDetectavel_DeveGerarPreviewVazio()
    {
        var extractor = new FakePhotoImportExtractor("");
        var service = CreateService(extractor);

        var preview = await service.AnalyzePhotoAsync(Bytes("img"), "selfie.jpg", "image/jpeg", CancellationToken.None);

        Assert.Empty(preview.Categories);
        Assert.Equal(0, preview.TotalRows);
    }

    [Fact]
    public async Task DevePassarNomeDoArquivoOriginalParaOExtractor()
    {
        var extractor = new FakePhotoImportExtractor("");
        var service = CreateService(extractor);

        await service.AnalyzePhotoAsync(Bytes("pdf-bytes"), "scan-camscanner.pdf", "application/pdf", CancellationToken.None);

        Assert.Equal("scan-camscanner.pdf", extractor.ReceivedFileName);
    }
}
