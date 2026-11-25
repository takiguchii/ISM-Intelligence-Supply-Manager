using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Telegram;

public sealed class TelegramRepository(IsmDbContext db) : ITelegramRepository
{
    public Task<TelegramConnection?> ForUserAsync(int userId, CancellationToken ct) => db.Set<TelegramConnection>().FirstOrDefaultAsync(x => x.UserId == userId, ct);
    public Task<TelegramConnection?> ForChatAsync(long chatId, CancellationToken ct) => db.Set<TelegramConnection>().FirstOrDefaultAsync(x => x.ChatId == chatId, ct);
    public Task<TelegramConnection?> ForCodeAsync(string hash, CancellationToken ct) => db.Set<TelegramConnection>().FirstOrDefaultAsync(x => x.CodeHash == hash, ct);
    public async Task SaveAsync(TelegramConnection connection, CancellationToken ct)
    {
        if (db.Entry(connection).State == EntityState.Detached) db.Add(connection);
        connection.Version++;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new InvalidOperationException("A conexão foi alterada por outra operação. Atualize e tente novamente."); }
    }
    public async Task<long> GetOffsetAsync(string bot, CancellationToken ct) =>
        (await db.Set<TelegramPollingCursor>().AsNoTracking().FirstOrDefaultAsync(x => x.BotUsername == bot, ct))?.Offset ?? 0;
    public async Task SetOffsetAsync(string bot, long offset, CancellationToken ct)
    {
        var cursor = await db.Set<TelegramPollingCursor>().FirstOrDefaultAsync(x => x.BotUsername == bot, ct);
        if (cursor == null) { cursor = new() { BotUsername = bot }; db.Add(cursor); }
        cursor.Offset = Math.Max(cursor.Offset, offset);
        await db.SaveChangesAsync(ct);
    }
}
