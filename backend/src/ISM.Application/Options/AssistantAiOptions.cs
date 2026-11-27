namespace ISM.Application.Options;

public sealed class AssistantAiOptions
{
    public const string SectionName = "AssistantAi";
    public bool Enabled { get; set; }
    public string OllamaBaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "gemma3:270m";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 1024;
    public int MaxContextItems { get; set; } = 12;
}
