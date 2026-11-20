using ISM.Domain.Modules.Assistant.Entities;

namespace ISM.Domain.Interfaces;

public interface IAssistantConversationRepository
{
    Task<IReadOnlyList<AssistantThread>> GetThreadsAsync(int restaurantId, int userId, int page, int limit, CancellationToken cancellationToken = default);
    Task<AssistantThread?> GetThreadAsync(int threadId, int restaurantId, int userId, CancellationToken cancellationToken = default);
    Task<AssistantThread> CreateThreadAsync(AssistantThread thread, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistantMessage>> GetMessagesAsync(int threadId, int restaurantId, int userId, int limit = 100, CancellationToken cancellationToken = default);
    Task<AssistantMessage> AddMessageAsync(AssistantMessage message, CancellationToken cancellationToken = default);
    Task SaveThreadAsync(AssistantThread thread, CancellationToken cancellationToken = default);
}
