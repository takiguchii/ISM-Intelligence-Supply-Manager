using ISM.Application.Interfaces.DataImport;
using ISM.Application.Services.DataImport;

namespace ISM.UnitTests;

public sealed class ReceiptPreviewServiceTests
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 1];

    [Fact]
    public async Task CompraConsistenteGeraSomentePreviaQueExigeConfirmacao()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"purchase","supplier":"Mercado","documentNumber":"N-1","issueDate":"2026-11-17","items":[{"description":"Arroz","quantity":1,"unit":"kg","unitPrice":22.9,"lineTotal":22.9}],"subtotal":22.9,"total":22.9,"uncertainFields":[]}"""));
        var result = await service.AnalyzeAsync(Png, "nota.png", "image/png", CancellationToken.None);

        Assert.Equal("PREVIEW_REQUIRES_CONFIRMATION", result.Decision);
        Assert.True(result.RequiresConfirmation);
        Assert.False(result.FiscalDocument);
        Assert.Empty(result.ValidationIssues);
        Assert.Equal(0m, result.Discount);
        Assert.Equal(64, result.Sha256.Length);
    }

    [Fact]
    public async Task DevolucaoNuncaViraEntradaDeEstoque()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"return","supplier":"Mercado","items":[],"subtotal":0,"discount":0,"total":0}"""));
        var result = await service.AnalyzeAsync(Png, "devolucao.png", "image/png", CancellationToken.None);

        Assert.Equal("BLOCK_NO_STOCK_ENTRY", result.Decision);
        Assert.True(result.RequiresConfirmation);
    }

    [Fact]
    public async Task DivergenciaMatematicaExigeRevisao()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"purchase","supplier":"Mercado","issueDate":"2026-11-17","items":[{"description":"Arroz","quantity":2,"unit":"kg","unitPrice":10,"lineTotal":20}],"subtotal":20,"discount":0,"total":18,"uncertainFields":[]}"""));
        var result = await service.AnalyzeAsync(Png, "nota.png", "image/png", CancellationToken.None);

        Assert.Equal("MANUAL_REVIEW_REQUIRED", result.Decision);
        Assert.Contains(result.ValidationIssues, issue => issue.Contains("Total ausente ou incompatível", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FornecedorMarcadoComoNaoDisponivelExigeRevisao()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"purchase","supplier":"N/A","issueDate":"2026-11-17","items":[{"description":"Arroz","quantity":1,"unit":"kg","unitPrice":10,"lineTotal":10}],"subtotal":10,"discount":0,"total":10,"uncertainFields":[]}"""));
        var result = await service.AnalyzeAsync(Png, "nota.png", "image/png", CancellationToken.None);

        Assert.Null(result.Supplier);
        Assert.Equal("MANUAL_REVIEW_REQUIRED", result.Decision);
        Assert.Contains(result.ValidationIssues, issue => issue.Contains("Fornecedor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CampoIncertoEmObjetoEConservadoComoRevisao()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"purchase","supplier":"Mercado","issueDate":"2026-11-17","items":[{"description":"Arroz","quantity":1,"unit":"kg","unitPrice":10,"lineTotal":10}],"subtotal":10,"discount":0,"total":10,"uncertainFields":[{"field":"issueDate"}]}"""));
        var result = await service.AnalyzeAsync(Png, "nota.png", "image/png", CancellationToken.None);

        Assert.Equal("MANUAL_REVIEW_REQUIRED", result.Decision);
        Assert.Equal(["issueDate"], result.UncertainFields);
    }

    [Fact]
    public async Task CompraSemNumeroDeDocumentoExigeRevisao()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"purchase","supplier":"Mercado","documentNumber":null,"issueDate":"2026-11-17","items":[{"description":"Arroz","quantity":1,"unit":"kg","unitPrice":10,"lineTotal":10}],"subtotal":10,"discount":0,"total":10,"uncertainFields":[]}"""));
        var result = await service.AnalyzeAsync(Png, "nota.png", "image/png", CancellationToken.None);

        Assert.Equal("MANUAL_REVIEW_REQUIRED", result.Decision);
        Assert.Contains(result.ValidationIssues, issue => issue.Contains("Número do documento", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DevolucaoComMoedaEQuantidadeBrasileiraNaoGeraEntrada()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"return","supplier":null,"documentNumber":"ISM-DEV-008","issueDate":"2026-11-12","items":[{"description":"QUEIJO MUSSARELA KG","quantity":"1,000 kg","unit":"kg","unitPrice":"34.00","lineTotal":"R$ 34,00"}],"subtotal":"R$ 34,00","discount":null,"total":"R$ 34,00","uncertainFields":[]}"""));
        var result = await service.AnalyzeAsync(Png, "devolucao.png", "image/png", CancellationToken.None);

        Assert.Equal(1m, result.Items[0].Quantity);
        Assert.Equal(34m, result.Total);
        Assert.Equal("BLOCK_NO_STOCK_ENTRY", result.Decision);
    }

    [Fact]
    public async Task DocumentoSemItensNuncaPodeVirarEntradaMesmoClassificadoComoCompra()
    {
        var service = new ReceiptPreviewService(new FakeReceiptExtractor("""{"documentKind":"purchase","supplier":null,"documentNumber":"REC-1","issueDate":"2026-11-17","items":[],"subtotal":0,"discount":0,"total":0,"uncertainFields":[]}"""));
        var result = await service.AnalyzeAsync(Png, "sem-itens.png", "image/png", CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal("BLOCK_NO_STOCK_ENTRY", result.Decision);
    }

    [Fact]
    public async Task BytesQueNaoCorrespondemAoMimeSaoRecusadosAntesDaInferencia()
    {
        var extractor = new FakeReceiptExtractor("{}");
        var service = new ReceiptPreviewService(extractor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync([1, 2, 3], "fake.png", "image/png", CancellationToken.None));
        Assert.False(extractor.Called);
    }

    private sealed class FakeReceiptExtractor(string json) : IReceiptVisionExtractor
    {
        public bool Called { get; private set; }
        public Task<string> ExtractReceiptJsonAsync(byte[] fileBytes, string fileName, string contentType, CancellationToken ct)
        {
            Called = true;
            return Task.FromResult(json);
        }
    }
}
