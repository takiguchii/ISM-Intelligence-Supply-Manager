namespace ISM.Application.Interfaces.Assistant;

public interface ILlmTextProvider
{
    Task<LlmTextResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

public sealed record LlmTextResult(bool Success, string? Text, string? Error);
