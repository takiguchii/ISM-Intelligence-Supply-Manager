using System.Net;
using System.Text;
using System.Text.Json;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Application.Services.DataImport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ISM.UnitTests;

public sealed class LlmVisionPhotoExtractorTests
{
    [Fact]
    public async Task OllamaRecebeImagemNoPayloadSemApiKey()
    {
        var handler = new CaptureHandler();
        var extractor = new LlmVisionPhotoExtractor(
            new HttpClient(handler),
            Options.Create(new PhotoImportOptions { ApiEndpoint = "http://127.0.0.1:11434", Model = "qwen3.5:4b" }),
            NullLogger<LlmVisionPhotoExtractor>.Instance,
            new TestCurrentUser(),
            null!);

        var result = await extractor.ExtractCsvAsync([1, 2, 3], "recibo.png", "image/png", CancellationToken.None);

        Assert.Equal("Item,Quantidade\nArroz,2", result);
        Assert.Equal("http://127.0.0.1:11434/api/chat", handler.RequestUri);
        Assert.Null(handler.Authorization);
        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("qwen3.5:4b", payload.RootElement.GetProperty("model").GetString());
        Assert.False(payload.RootElement.GetProperty("think").GetBoolean());
        Assert.Equal("AQID", payload.RootElement.GetProperty("messages")[1].GetProperty("images")[0].GetString());
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public string? Authorization { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"message":{"content":"Item,Quantidade\nArroz,2"}}""", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public int UserId => 1;
        public string? Email => null;
        public string Role => "Manager";
        public int? RestaurantId => null;
        public bool IsSuperAdmin => false;
        public bool IsManagerOrAbove => true;
    }
}
