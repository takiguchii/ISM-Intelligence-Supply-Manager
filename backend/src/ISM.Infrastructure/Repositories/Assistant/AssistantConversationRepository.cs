using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Assistant;

public sealed class AssistantConversationRepository(IsmDbContext db) : IAssistantConversationRepository
{
    public async Task<IReadOnlyList<AssistantThread>> GetThreadsAsync(int restaurantId, int userId, int page, int limit, CancellationToken cancellationToken = default)
        => await db.Set<AssistantThread>().AsNoTracking()
            .Where(t => t.RestaurantId == restaurantId && t.UserId == userId && !t.IsArchived)
            .OrderByDescending(t => t.LastMessageAtUtc)
            .Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);

    public Task<AssistantThread?> GetThreadAsync(int threadId, int restaurantId, int userId, CancellationToken cancellationToken = default)
        => db.Set<AssistantThread>().FirstOrDefaultAsync(t => t.Id == threadId && t.RestaurantId == restaurantId && t.UserId == userId && !t.IsArchived, cancellationToken);

    public async Task<AssistantThread> CreateThreadAsync(AssistantThread thread, CancellationToken cancellationToken = default)
    {
        db.Set<AssistantThread>().Add(thread);
        await db.SaveChangesAsync(cancellationToken);
        return thread;
    }

    public async Task<IReadOnlyList<AssistantMessage>> GetMessagesAsync(int threadId, int restaurantId, int userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var messages = await db.Set<AssistantMessage>().AsNoTracking()
            .Where(m => m.ThreadId == threadId && m.RestaurantId == restaurantId && (m.UserId == userId || m.UserId == null))
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(cancellationToken);
        return messages.OrderBy(m => m.CreatedAtUtc).ToList();
    }

    public async Task<AssistantMessage> AddMessageAsync(AssistantMessage message, CancellationToken cancellationToken = default)
    {
        db.Set<AssistantMessage>().Add(message);
        await db.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task SaveThreadAsync(AssistantThread thread, CancellationToken cancellationToken = default)
    {
        db.Set<AssistantThread>().Update(thread);
        await db.SaveChangesAsync(cancellationToken);
    }
}
