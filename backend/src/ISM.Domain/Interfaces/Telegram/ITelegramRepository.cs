using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface ITelegramRepository
{
    Task<TelegramConnection?> ForUserAsync(int userId, CancellationToken ct);
    Task<TelegramConnection?> ForChatAsync(long chatId, CancellationToken ct);
    Task<TelegramConnection?> ForCodeAsync(string hash, CancellationToken ct);
    Task SaveAsync(TelegramConnection connection, CancellationToken ct);
    Task<long> GetOffsetAsync(string bot, CancellationToken ct);
    Task SetOffsetAsync(string bot, long offset, CancellationToken ct);
}
