using ISM.Application.DTOs.Assistant;

namespace ISM.Application.Interfaces.Assistant;

public interface IAssistantService
{
    Task<AssistantStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<AssistantOverviewResponse> GetOverviewAsync(int page, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistantActionResponse>> GetActionsAsync(int page, int limit, CancellationToken cancellationToken = default);
    Task<AssistantActionResponse?> GetActionAsync(int id, CancellationToken cancellationToken = default);
    Task<(AssistantActionResponse? Action, int StatusCode)> ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task<AssistantActionResponse?> RejectAsync(int id, CancellationToken cancellationToken = default);
    Task<AssistantActionResponse?> ReviewAsync(int id, CancellationToken cancellationToken = default);
    Task<AssistantChatResponse> ChatAsync(string message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistantThreadResponse>> GetThreadsAsync(int page, int limit, CancellationToken cancellationToken = default);
    Task<AssistantConversationResponse?> CreateThreadAsync(AssistantCreateThreadRequest request, CancellationToken cancellationToken = default);
    Task<AssistantConversationResponse?> GetThreadAsync(int threadId, CancellationToken cancellationToken = default);
    Task<bool> ArchiveThreadAsync(int threadId, CancellationToken cancellationToken = default);
    Task<AssistantContextResponse?> GetAlertContextAsync(int alertId, CancellationToken cancellationToken = default);
    Task<AssistantConversationResponse?> SendMessageAsync(
        int threadId,
        string message,
        CancellationToken cancellationToken = default,
        byte[]? imageBytes = null,
        string? imageContentType = null,
        string? imageFileName = null);
}
