using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Extrator que usa um LLM com visão (API compatível com OpenAI) para converter
/// fotos de cadernos, PDFs escaneados e prints em CSV estruturado.
/// </summary>
public sealed class LlmVisionPhotoExtractor : IPhotoImportExtractor
{
    private readonly HttpClient _httpClient;
    private readonly PhotoImportOptions _options;
    private readonly ILogger<LlmVisionPhotoExtractor> _logger;

    private const string SystemPrompt =
        """
        Você é um extrator de dados tabulares para um sistema de gestão de restaurantes (ISM).
        Receba uma imagem ou documento escaneado e devolva APENAS o conteúdo em CSV,
        sem explicações, sem markdown, sem blocos de código.

        Regras:
        - Primeira linha: cabeçalhos de coluna em português, descritivos (ex: Nome, Unidade, Quantidade Atual, Custo).
        - Uma linha por registro da tabela.
        - Valores monetários com ponto decimal. Datas no formato yyyy-MM-dd.
        - Células ilegíveis: deixe vazias.
        - Ignore rodapés, logos, carimbos e texto fora da tabela.
        - Se a imagem não contiver nenhuma tabela, devolva apenas: ERRO_SEM_TABELA
        """;

    public LlmVisionPhotoExtractor(
        HttpClient httpClient,
        IOptions<PhotoImportOptions> options,
        ILogger<LlmVisionPhotoExtractor> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExtractCsvAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "Extração por foto indisponível: configure a chave da API (env var PHOTO_IMPORT__APIKEY ou seção PhotoImport).");

        var dataUri = $"data:{contentType};base64,{Convert.ToBase64String(fileBytes)}";

        var payload = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = $"Extraia a tabela deste arquivo ({fileName}) como CSV." },
                        new { type = "image_url", image_url = new { url = dataUri } }
                    }
                }
            },
            max_tokens = 4096,
            temperature = 0
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiEndpoint.TrimEnd('/')}/chat/completions");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Falha na extração via visão: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Provedor de visão retornou {(int)response.StatusCode}. Tente novamente com imagem mais nítida.");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim() ?? string.Empty;

        if (text.StartsWith("```"))
            text = StripCodeFence(text);

        if (text.Contains("ERRO_SEM_TABELA") || text.Length == 0)
            throw new InvalidOperationException(
                "Não foi possível identificar uma tabela nesta imagem. Tente uma foto mais nítida, com a folha inteira visível.");

        return text;
    }

    private static string StripCodeFence(string text)
    {
        var lines = text.Split('\n');
        var start = lines[0].StartsWith("```") ? 1 : 0;
        var end = lines.Length > start && lines[^1].Trim().StartsWith("```") ? lines.Length - 1 : lines.Length;
        return string.Join('\n', lines[start..end]);
    }
}
