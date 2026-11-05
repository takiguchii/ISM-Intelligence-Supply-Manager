using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Users;

public sealed class AuthSessionRepository(IsmDbContext db) : IAuthSessionRepository
{
    public async Task AddAsync(AuthSession session, CancellationToken ct = default)
    {
        db.Set<AuthSession>().Add(session);
        await db.SaveChangesAsync(ct);
    }

    public Task<AuthSession?> FindAsync(string id, CancellationToken ct = default) =>
        db.Set<AuthSession>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<AuthSession>> ListAsync(int userId, CancellationToken ct = default) =>
        await db.Set<AuthSession>().AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAtUtc).ToListAsync(ct);

    public async Task RevokeAsync(int userId, string? sessionId, CancellationToken ct = default)
    {
        var sessions = await db.Set<AuthSession>().Where(s => s.UserId == userId &&
            (sessionId == null || s.Id == sessionId) && s.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var session in sessions) session.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
