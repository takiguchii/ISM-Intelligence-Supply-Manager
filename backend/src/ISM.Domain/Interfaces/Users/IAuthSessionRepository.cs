using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface IAuthSessionRepository
{
    Task AddAsync(AuthSession session, CancellationToken ct = default);
    Task<AuthSession?> FindAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<AuthSession>> ListAsync(int userId, CancellationToken ct = default);
    Task RevokeAsync(int userId, string? sessionId, CancellationToken ct = default);
}
