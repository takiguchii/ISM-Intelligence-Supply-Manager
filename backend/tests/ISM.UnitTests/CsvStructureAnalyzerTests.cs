using System.Text;
using ISM.Application.Services.DataImport;

namespace ISM.UnitTests;

public class CsvStructureAnalyzerTests
{
    private static CsvStructureAnalyzer CreateAnalyzer() => new(
    [
        new StockCategoryProfile(),
        new SuppliersCategoryProfile(),
        new FinanceCategoryProfile()
    ]);

    private static Stream Csv(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task DeveClassificarPlanilhaDeEstoque()
    {
        var analyzer = CreateAnalyzer();
        var csv = "Nome,Unidade,QuantidadeAtual,QuantidadeMinima,CustoMedio\n" +
                  "Arroz,kg,50,10,5.90\n";

        var preview = await analyzer.AnalyzeAsync(Csv(csv), "estoque.csv", "text/csv", CancellationToken.None);

        Assert.Equal(1, preview.TotalRows);
        Assert.Single(preview.Categories);

        var estoque = preview.Categories[0];
        Assert.Equal("estoque", estoque.Category);
        Assert.Equal("Product", estoque.TargetEntity);
        Assert.True(estoque.CanImport);
        Assert.Equal("Arroz", estoque.MappedFields[0]["nome"]);
        Assert.Equal("kg", estoque.MappedFields[0]["unidade"]);
    }

    [Fact]
    public async Task DeveClassificarPlanilhaDeFinancas()
    {
        var analyzer = CreateAnalyzer();
        var csv = "Data,Descricao,Tipo,Valor\n" +
                  "2025-08-01,Venda balcão,receita,1500.00\n" +
                  "2025-08-02,Compra de insumos,despesa,300.00\n";

        var preview = await analyzer.AnalyzeAsync(Csv(csv), "financas.csv", "text/csv", CancellationToken.None);

        Assert.Contains(preview.Categories, c => c.Category == "financas");
        var financas = preview.Categories.First(c => c.Category == "financas");
        Assert.True(financas.CanImport);
        Assert.Equal(2, financas.RowCount);
    }

    [Fact]
    public async Task LinhaInvalidaDeveAparecerNaConfirmacao()
    {
        var analyzer = CreateAnalyzer();
        var csv = "Nome,Unidade,QuantidadeAtual\n" +
                  ",kg,10\n"; // nome obrigatório ausente

        var preview = await analyzer.AnalyzeAsync(Csv(csv), "estoque.csv", "text/csv", CancellationToken.None);

        var estoque = preview.Categories.First(c => c.Category == "estoque");
        Assert.False(estoque.CanImport);
        Assert.Contains(2, estoque.RowsWithErrors); // header é a linha 1, dado é a linha 2
    }

    [Fact]
    public async Task ColunasNaoReconhecidasDevemFicarUnmapped()
    {
        var analyzer = CreateAnalyzer();
        var csv = "Nome,CampoDesconhecido\nArroz,x\n";

        var preview = await analyzer.AnalyzeAsync(Csv(csv), "planilha.csv", "text/csv", CancellationToken.None);

        Assert.Contains("CampoDesconhecido", preview.UnmappedColumns);
    }

    [Fact]
    public async Task PlanilhaSemCategoriaConhecidaNaoDeveTerCategorias()
    {
        var analyzer = CreateAnalyzer();
        var csv = "ColunaA\nvalor\n";

        var preview = await analyzer.AnalyzeAsync(Csv(csv), "aleatorio.csv", "text/csv", CancellationToken.None);

        Assert.Empty(preview.Categories);
    }
}
