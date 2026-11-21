namespace ISM.Application.Interfaces.Assistant;

public interface ILlmTextProvider
{
    Task<LlmTextResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    Task<LlmTextResult> CompleteWithImageAsync(string systemPrompt, string userPrompt, byte[] imageBytes, string contentType, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<LlmProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed record LlmTextResult(bool Success, string? Text, string? Error);
public sealed record LlmProviderStatus(bool Enabled, bool Reachable, string Provider, string Model, string? Error);
