using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.Services.DataImport.Readers;

namespace ISM.UnitTests;

public class XlsxImportFileReaderTests
{
    private static Stream BuildWorkbook(object?[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Estoque");
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 1, c + 1).Value = XLCellValue.FromObject(rows[r][c] ?? string.Empty);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task DeveLerPlanilhaXlsxComCabecalhosELinhas()
    {
        var reader = new XlsxImportFileReader();
        var stream = BuildWorkbook(
        [
            ["Nome", "Unidade", "QuantidadeAtual", "CustoMedio"],
            ["Arroz", "kg", 50, 5.9],
            ["Feijão", "kg", 30, 7.25],
        ]);

        var content = await reader.ReadAsync(stream, CancellationToken.None);

        Assert.Equal(["Nome", "Unidade", "QuantidadeAtual", "CustoMedio"], content.Headers);
        Assert.Equal(2, content.Rows.Count);
        Assert.Equal("Arroz", content.Rows[0]["Nome"]);
        Assert.Equal("50", content.Rows[0]["QuantidadeAtual"]);
        Assert.Equal("5.9", content.Rows[1]["CustoMedio"]);
    }

    [Fact]
    public void CanHandle_ReconheceExtensaoXlsx()
    {
        var reader = new XlsxImportFileReader();
        Assert.True(reader.CanHandle("planilha.xlsx", null));
        Assert.False(reader.CanHandle("dados.csv", null));
    }
}

public class NFeXmlImportFileReaderTests
{
    private const string NfeXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <NFe xmlns="http://www.portalfiscal.inf.br/nfe">
          <infNFe Id="NFe352508..." versao="4.00">
            <emit><xNome>Distribuidora Teste</xNome></emit>
            <det nItem="1"><prod>
              <xProd>Arroz Tipo 1 5kg</xProd><uCom>un</uCom>
              <qCom>20.0000</qCom><vUnCom>28.9000</vUnCom>
            </prod></det>
            <det nItem="2"><prod>
              <xProd>Oleo de Soja 900ml</xProd><uCom>UN</uCom>
              <qCom>48.0000</qCom><vUnCom>6.2000</vUnCom>
            </prod></det>
          </infNFe>
        </NFe>
        """;

    [Fact]
    public async Task DeveExtrairProdutosDaNFeComoLinhasDeEstoque()
    {
        var reader = new NFeXmlImportFileReader();

        var content = await reader.ReadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(NfeXml)), CancellationToken.None);

        Assert.Equal(["Nome", "Unidade", "QuantidadeAtual", "CustoMedio"], content.Headers);
        Assert.Equal(2, content.Rows.Count);
        Assert.Equal("Arroz Tipo 1 5kg", content.Rows[0]["Nome"]);
        Assert.Equal("un", content.Rows[0]["Unidade"]);
        Assert.Equal("6.2000", content.Rows[1]["CustoMedio"]);
    }

    [Fact]
    public async Task XmlSemItens_DeveLancarErroAmigavel()
    {
        var reader = new NFeXmlImportFileReader();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadAsync(new MemoryStream(Encoding.UTF8.GetBytes("<root><a>1</a></root>")),
                CancellationToken.None));
    }
}

public class SpreadSheetMlImportFileReaderTests
{
    private const string SmlXml = """
        <?xml version="1.0"?>
        <Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
                  xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
          <Worksheet ss:Name="Estoque">
            <Table>
              <Row>
                <Cell><Data ss:Type="String">Nome</Data></Cell>
                <Cell ss:Index="3"><Data ss:Type="String">QuantidadeAtual</Data></Cell>
              </Row>
              <Row>
                <Cell><Data ss:Type="String">Arroz</Data></Cell>
                <Cell ss:Index="3"><Data ss:Type="Number">50</Data></Cell>
              </Row>
            </Table>
          </Worksheet>
        </Workbook>
        """;

    [Fact]
    public async Task DevePreencherColunasPuladasComVazio()
    {
        var reader = new SpreadSheetMlImportFileReader();

        var content = await reader.ReadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(SmlXml)), CancellationToken.None);

        Assert.Equal(3, content.Headers.Count); // Coluna2 foi pulada via ss:Index
        Assert.Equal(1, content.Rows.Count);
        Assert.Equal("Arroz", content.Rows[0]["Nome"]);
        Assert.Equal(string.Empty, content.Rows[0]["Coluna2"]);
        Assert.Equal("50", content.Rows[0]["QuantidadeAtual"]);
    }
}

public class JsonWebhookImportFileReaderTests
{
    private const string JsonPayload = """
        {
          "origem": "PDV",
          "pedidos": [
            {
              "externalOrderId": "001",
              "orderedAtUtc": "2025-08-01T18:32:00Z",
              "canal": "balcao",
              "status": "Paid",
              "itens": [
                { "prato": "Prato Feito", "quantidade": 2, "valorUnitario": 28.00 },
                { "prato": "Refrigerante", "quantidade": 3, "valorUnitario": 6.00 }
              ]
            },
            {
              "externalOrderId": "002",
              "orderedAtUtc": "2025-08-01T20:00:00Z",
              "canal": "ifood",
              "status": "Canceled",
              "itens": [{ "prato": "Pizza", "quantidade": 1, "valorUnitario": 40.00 }]
            },
            {
              "externalOrderId": "003",
              "canal": "ifood",
              "itens": [{ "prato": "Lasanha", "quantidade": 2, "valorUnitario": 35.50 }],
              "taxaEntrega": 10.00
            }
          ]
        }
        """;

    [Fact]
    public async Task DeveConverterPedidosEmLinhasFinanceiras_IgnorandoCancelados()
    {
        var reader = new JsonWebhookImportFileReader();

        var content = await reader.ReadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(JsonPayload)), CancellationToken.None);

        Assert.Equal(["Data", "Historico", "Tipo", "Valor", "FormaPagamento"], content.Headers);
        Assert.Equal(2, content.Rows.Count); // pedido cancelado foi ignorado
        Assert.Equal("balcao/001", content.Rows[0]["Historico"]);
        Assert.Equal("receita", content.Rows[0]["Tipo"]);
        Assert.Equal("74", content.Rows[0]["Valor"]); // 2*28 + 3*6
        Assert.Equal("81", content.Rows[1]["Valor"]); // 2*35.5 + 10 taxa
    }
}

public class ImportFileReaderResolverTests
{
    private static IImportFileReaderResolver CreateResolver() => new ImportFileReaderResolver(
    [
        new CsvImportFileReader(),
        new XlsxImportFileReader(),
        new NFeXmlImportFileReader(),
        new SpreadSheetMlImportFileReader(),
        new JsonWebhookImportFileReader()
    ]);

    private static Stream Bytes(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

    [Fact]
    public async Task Csv_DeveSerLidoDiretamente()
    {
        var preview = await CreateResolver().ReadAsync(
            Bytes("Nome\nArroz\n"), "estoque.csv", "text/csv", CancellationToken.None);
        Assert.Single(preview.Rows);
    }

    [Fact]
    public async Task XlsxBinario_DeveSerRoteadoParaReaderExcel()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("S");
        ws.Cell(1, 1).Value = "Nome";
        ws.Cell(2, 1).Value = "Arroz";
        var ms = new MemoryStream();
        wb.SaveAs(ms);

        var content = await CreateResolver().ReadAsync(ms, "planilha.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", CancellationToken.None);

        Assert.Single(content.Rows);
        Assert.Equal("Arroz", content.Rows[0]["Nome"]);
    }

    [Fact]
    public async Task NfeXml_DeveSerDesambiguadoDoSpreadsheetMl()
    {
        var resolver = CreateResolver();
        var nfe = "<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infNFe><det><prod>" +
                  "<xProd>Teste</xProd></prod></det></infNFe></NFe>";

        var content = await resolver.ReadAsync(Bytes(nfe), "nota.xml", "application/xml",
            CancellationToken.None);

        Assert.Single(content.Rows);
        Assert.Equal("Teste", content.Rows[0]["Nome"]);
    }

    [Fact]
    public async Task XmlDesconhecido_DeveLancarErroAmigavel()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateResolver().ReadAsync(
                Bytes("<configuracoes><tema>escuro</tema></configuracoes>"),
                "config.xml", "application/xml", CancellationToken.None));

        Assert.Contains("não suportado", ex.Message);
    }

    [Fact]
    public async Task FormatoDesconhecido_CaiNoFallbackCsv()
    {
        // Arquivo vazio/ilegível não deve lançar exceção — comporta-se como CSV sem dados
        var content = await CreateResolver().ReadAsync(
            Bytes(""), "arquivo.desconhecido", null,
            CancellationToken.None);

        Assert.Empty(content.Rows);
        Assert.Empty(content.Headers);
    }
}
