using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ISM.Application.Options;

public static class AiProviderCatalog
{
    public const string OpenAI = "OpenAI";
    public const string OpenCodeGo = "OpenCodeGo";
    public const string Gemini = "Gemini";
    public const string Anthropic = "Anthropic";
    public const string DeepSeek = "DeepSeek";
    public const string OpenRouter = "OpenRouter";
    public const string Groq = "Groq";
    public const string Mistral = "Mistral";

    public static string Normalize(string? provider, string fallback = OpenAI)
    {
        var value = provider?.Trim();
        if (string.IsNullOrEmpty(value)) return fallback;
        if (value.Equals("Codex", StringComparison.OrdinalIgnoreCase) || value.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)) return OpenAI;
        if (value.Equals("OpenCode", StringComparison.OrdinalIgnoreCase) || value.Equals("OpenCodeGo", StringComparison.OrdinalIgnoreCase) || value.Equals("OpenCode Go", StringComparison.OrdinalIgnoreCase)) return OpenCodeGo;
        if (value.Equals("Google", StringComparison.OrdinalIgnoreCase) || value.Equals("Gemini", StringComparison.OrdinalIgnoreCase)) return Gemini;
        if (value.Equals("Claude", StringComparison.OrdinalIgnoreCase) || value.Equals("Anthropic", StringComparison.OrdinalIgnoreCase)) return Anthropic;
        if (value.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase)) return DeepSeek;
        if (value.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)) return OpenRouter;
        if (value.Equals("Groq", StringComparison.OrdinalIgnoreCase)) return Groq;
        if (value.Equals("Mistral", StringComparison.OrdinalIgnoreCase)) return Mistral;
        throw new InvalidOperationException("Provedor inválido. Escolha OpenAI/Codex, OpenCode Go, Gemini, Anthropic, DeepSeek, OpenRouter, Groq ou Mistral.");
    }

    public static string Endpoint(string provider) => Normalize(provider) switch
    {
        OpenAI => "https://api.openai.com/v1",
        OpenCodeGo => "https://opencode.ai/inference/go/openai/v1",
        Gemini => "https://generativelanguage.googleapis.com/v1beta",
        Anthropic => "https://api.anthropic.com/v1",
        DeepSeek => "https://api.deepseek.com/v1",
        OpenRouter => "https://openrouter.ai/api/v1",
        Groq => "https://api.groq.com/openai/v1",
        Mistral => "https://api.mistral.ai/v1",
        _ => throw new InvalidOperationException("Provedor de IA não suportado.")
    };

    public static string DefaultModel(string provider) => Normalize(provider) switch
    {
        OpenAI => "gpt-6.2-astra",
        OpenCodeGo => "deepseek-v4.1-flash",
        Gemini => "gemini-3.5-flash-lite",
        Anthropic => "claude-sonnet-4-5",
        DeepSeek => "deepseek-chat",
        OpenRouter => "openai/gpt-4o-mini",
        Groq => "llama-3.3-70b-versatile",
        Mistral => "mistral-small-latest",
        _ => ""
    };

    public static HttpRequestMessage CreateRequest(
        string provider,
        string model,
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        byte[]? imageBytes = null,
        string? contentType = null,
        bool jsonOutput = false,
        string? sessionId = null)
    {
        provider = Normalize(provider);
        var endpoint = Endpoint(provider);
        var encodedImage = imageBytes is null ? null : Convert.ToBase64String(imageBytes);
        var dataUri = encodedImage is null ? null : $"data:{contentType ?? "image/jpeg"};base64,{encodedImage}";

        HttpRequestMessage request;
        object payload;
        switch (provider)
        {
            case OpenAI:
                request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/responses");
                payload = new
                {
                    model,
                    instructions = systemPrompt,
                    max_output_tokens = maxOutputTokens,
                    input = imageBytes is null
                        ? JsonSerializer.SerializeToElement(userPrompt)
                        : JsonSerializer.SerializeToElement(new[] { new { role = "user", content = new object[] { new { type = "input_text", text = userPrompt }, new { type = "input_image", image_url = dataUri } } } })
                };
                AddBearer(request, apiKey);
                break;
            case OpenCodeGo:
            case DeepSeek:
            case OpenRouter:
            case Groq:
            case Mistral:
                request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/chat/completions");
                payload = new
                {
                    model,
                    max_tokens = maxOutputTokens,
                    messages = imageBytes is null
                        ? new object[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = userPrompt }
                        }
                        : new object[]
                        {
                            new { role = "system", content = systemPrompt },
                            new
                            {
                                role = "user",
                                content = new object[]
                                {
                                    new { type = "text", text = userPrompt },
                                    new { type = "image_url", image_url = new { url = dataUri } }
                                }
                            }
                        }
                };
                AddBearer(request, apiKey);
                if (provider == OpenCodeGo && !string.IsNullOrWhiteSpace(sessionId)) request.Headers.TryAddWithoutValidation("x-opencode-session", sessionId);
                break;
            case Gemini:
                request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/models/{Uri.EscapeDataString(model)}:generateContent");
                payload = new
                {
                    systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[] { new { role = "user", parts = imageBytes is null
                        ? new object[] { new { text = userPrompt } }
                        : new object[] { new { text = userPrompt }, new { inline_data = new { mime_type = contentType ?? "image/jpeg", data = encodedImage } } } } },
                    generationConfig = new { maxOutputTokens = maxOutputTokens, responseMimeType = jsonOutput ? "application/json" : null }
                };
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
                break;
            case Anthropic:
                request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/messages");
                payload = new
                {
                    model,
                    max_tokens = maxOutputTokens,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = imageBytes is null
                        ? JsonSerializer.SerializeToElement(userPrompt)
                        : JsonSerializer.SerializeToElement(new object[] { new { type = "text", text = userPrompt }, new { type = "image", source = new { type = "base64", media_type = contentType ?? "image/jpeg", data = encodedImage } } }) } }
                };
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
                request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
                break;
            default:
                throw new InvalidOperationException("Provedor de IA não suportado.");
        }

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return request;
    }

    public static async Task<string?> ExtractTextAsync(HttpResponseMessage response, string provider, CancellationToken ct)
    {
        return ExtractText(provider, await response.Content.ReadAsStringAsync(ct));
    }

    public static string? ExtractText(string provider, string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        switch (Normalize(provider))
        {
            case OpenAI:
                if (root.TryGetProperty("output_text", out var outputText)) return outputText.GetString();
                if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
                    foreach (var item in output.EnumerateArray())
                        if (item.TryGetProperty("content", out var contents) && contents.ValueKind == JsonValueKind.Array)
                            foreach (var part in contents.EnumerateArray())
                                if (part.TryGetProperty("text", out var text)) return text.GetString();
                break;
            case Gemini:
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0 && candidates[0].TryGetProperty("content", out var geminiContent) && geminiContent.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0 && parts[0].TryGetProperty("text", out var geminiText)) return geminiText.GetString();
                break;
            case Anthropic:
                if (root.TryGetProperty("content", out var anthropicContent) && anthropicContent.GetArrayLength() > 0 && anthropicContent[0].TryGetProperty("text", out var anthropicText)) return anthropicText.GetString();
                break;
            default:
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0 && choices[0].TryGetProperty("message", out var message) && message.TryGetProperty("content", out var choiceText)) return choiceText.GetString();
                break;
        }
        return null;
    }

    private static void AddBearer(HttpRequestMessage request, string? apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
}
