using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using ISM.Application.Security;
using ISM.Application.DTOs.DataImport;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.IntegrationTests;
public sealed class ProductImportTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;
    public ProductImportTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SupplierCsv_PersistsSupplierAndDoesNotCreateStockProduct()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Manager, 931));
        const string csv = "Nome,Categoria,Descricao,Email\nDEMO Horta do Bairro,Hortifrúti,Fornecedor fictício,horta@example.invalid\n";
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "fornecedores-demonstracao.csv");
        var response = await client.PostAsync("/api/import/suppliers", form);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<ImportResultDto>())!;
        result.RecordsSucceeded.Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        (await db.Suppliers.IgnoreQueryFilters().AnyAsync(s => s.RestaurantId == 931 && s.Name == "DEMO Horta do Bairro")).Should().BeTrue();
        (await db.Products.IgnoreQueryFilters().AnyAsync(p => p.RestaurantId == 931 && p.Name == "DEMO Horta do Bairro")).Should().BeFalse();
    }

    [Fact]
    public async Task NfeDecimals_ArePersistedWithoutChangingScale()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FoundationWebApplicationFactory.Token(IsmRoles.Manager, 930));
        const string xml = """
            <NFe xmlns="http://www.portalfiscal.inf.br/nfe"><infNFe Id="NFe352508"><emit><xNome>Fornecedor Teste</xNome></emit><det nItem="1"><prod><xProd>Queijo Teste</xProd><uCom>KG</uCom><qCom>15.0000</qCom><vUnCom>42.5000</vUnCom></prod></det></infNFe></NFe>
            """;
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(xml));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        form.Add(file, "file", "nota.xml");
        var result = await client.PostAsync("/api/import/stock/products", form);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        var product = await db.Products.IgnoreQueryFilters().SingleAsync(p => p.RestaurantId == 930 && p.Name == "Queijo Teste");
        product.CurrentQuantity.Should().Be(15m);
        product.AverageCost.Should().Be(42.5m);
    }
}
