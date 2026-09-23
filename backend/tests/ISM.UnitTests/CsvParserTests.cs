using System.Text;
using ISM.Application.Services.DataImport;

namespace ISM.UnitTests;

public class CsvParserTests
{
    private static Stream Csv(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ReadRowsAsync_DeveLerLinhasComColunasNomeadas()
    {
        var (rows, errors) = await CsvParser.ReadRowsAsync(
            Csv("Nome,Unidade\nArroz,kg\nFeijão,L\n"), CancellationToken.None);

        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Arroz", rows[0]["Nome"]);
        Assert.Equal("kg", rows[0]["Unidade"]);
        Assert.Equal("Feijão", rows[1]["Nome"]);
    }

    [Fact]
    public async Task ReadRowsAsync_DeveTratarVirgulaDentroDeAspas()
    {
        var (rows, errors) = await CsvParser.ReadRowsAsync(
            Csv("Descricao,Valor\n\"Óleo de Soja, 900ml\",\"7,50\"\n"), CancellationToken.None);

        Assert.Empty(errors);
        Assert.Single(rows);
        Assert.Equal("Óleo de Soja, 900ml", rows[0]["Descricao"]);
        Assert.Equal("7,50", rows[0]["Valor"]);
    }

    [Fact]
    public async Task ReadRowsAsync_DeveTratarAspasEscapadas()
    {
        var (rows, errors) = await CsvParser.ReadRowsAsync(
            Csv("Descricao\n\"Produto \"\"Especial\"\"\"\n"), CancellationToken.None);

        Assert.Empty(errors);
        Assert.Equal("Produto \"Especial\"", rows[0]["Descricao"]);
    }

    [Fact]
    public async Task ReadRowsAsync_ColunaFaltanteDeveVirVazia()
    {
        var (rows, _) = await CsvParser.ReadRowsAsync(
            Csv("A,B,C\nvalor1,valor2\n"), CancellationToken.None);

        Assert.Single(rows);
        Assert.Equal(string.Empty, rows[0]["C"]);
    }

    [Fact]
    public void IsCsv_DeveReconhecerPorExtensaoEContentType()
    {
        Assert.True(CsvParser.IsCsv(null, "estoque.csv"));
        Assert.True(CsvParser.IsCsv("text/csv", null));
        Assert.False(CsvParser.IsCsv("application/json", "dados.json"));
    }
}
