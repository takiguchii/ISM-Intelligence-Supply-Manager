using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Assistant.Entities;
using ISM.Domain.Modules.Assistant.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Assistant;

public sealed class AssistantRepository(IsmDbContext db) : IAssistantRepository
{
    public Task<AssistantProposal?> GetProposalAsync(int id, int restaurantId, CancellationToken cancellationToken = default)
        => db.Set<AssistantProposal>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.RestaurantId == restaurantId, cancellationToken);

    public async Task<IReadOnlyList<AssistantProposal>> GetProposalsAsync(int restaurantId, int page, int limit, CancellationToken cancellationToken = default)
        => await db.Set<AssistantProposal>().AsNoTracking().Where(p => p.RestaurantId == restaurantId)
            .OrderBy(p => p.Status).ThenByDescending(p => p.GeneratedAtUtc).Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);

    public Task<AssistantProposal?> FindByAlertAsync(int alertId, int restaurantId, CancellationToken cancellationToken = default)
        => db.Set<AssistantProposal>().FirstOrDefaultAsync(p => p.AlertId == alertId && p.RestaurantId == restaurantId, cancellationToken);

    public async Task<AssistantProposal> CreateProposalAsync(AssistantProposal proposal, CancellationToken cancellationToken = default)
    {
        try
        {
            db.Add(proposal);
            await db.SaveChangesAsync(cancellationToken);
            return proposal;
        }
        catch (DbUpdateException)
        {
            db.Entry(proposal).State = EntityState.Detached;
            return await db.Set<AssistantProposal>().SingleAsync(p => p.AlertId == proposal.AlertId && p.RestaurantId == proposal.RestaurantId, cancellationToken);
        }
    }

    public async Task ExpirePendingAsync(int restaurantId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var proposals = await db.Set<AssistantProposal>()
            .Where(p => p.RestaurantId == restaurantId && p.Status == AssistantProposalStatus.Pending && p.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
        if (proposals.Count == 0) return;

        foreach (var proposal in proposals)
        {
            proposal.Status = AssistantProposalStatus.Expired;
            proposal.UpdatedAtUtc = nowUtc;
            db.Set<AssistantActionAudit>().Add(new AssistantActionAudit
            {
                RestaurantId = restaurantId, ProposalId = proposal.Id, ActorUserId = 0,
                Action = "Expired", EntityType = proposal.EntityType, EntityId = proposal.EntityId,
                ValueBefore = proposal.OriginalValue, ValueAfter = proposal.OriginalValue,
                DetailsJson = proposal.EvidenceJson, OccurredAtUtc = nowUtc
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssistantApprovalResult> ApprovePriceAsync(int proposalId, int restaurantId, int userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var proposal = await db.Set<AssistantProposal>().FirstOrDefaultAsync(p => p.Id == proposalId && p.RestaurantId == restaurantId, cancellationToken);
        if (proposal is null) return new(null, false, false, null);
        if (proposal.Status != AssistantProposalStatus.Pending)
            return new(proposal, false, true, null);
        if (proposal.ExpiresAtUtc <= DateTime.UtcNow)
        {
            proposal.Status = AssistantProposalStatus.Expired;
            proposal.UpdatedAtUtc = DateTime.UtcNow;
            db.Set<AssistantActionAudit>().Add(new AssistantActionAudit
            {
                RestaurantId = restaurantId, ProposalId = proposal.Id, ActorUserId = 0,
                Action = "Expired", EntityType = proposal.EntityType, EntityId = proposal.EntityId,
                ValueBefore = proposal.OriginalValue, ValueAfter = proposal.OriginalValue,
                DetailsJson = proposal.EvidenceJson, OccurredAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(proposal, false, true, null);
        }

        var dish = await db.Dishes.FirstOrDefaultAsync(d => d.Id == proposal.EntityId && d.RestaurantId == restaurantId, cancellationToken);
        if (dish is null) return new(proposal, true, false, null);
        if (dish.Price != proposal.OriginalValue || dish.PriceRevision != proposal.Version)
        {
            proposal.Status = AssistantProposalStatus.Stale;
            proposal.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(proposal, true, false, dish.Price);
        }

        dish.Price = proposal.ProposedValue;
        dish.PriceRevision++;
        dish.UpdatedAtUtc = DateTime.UtcNow;
        proposal.Status = AssistantProposalStatus.Approved;
        proposal.Version++;
        proposal.DecidedByUserId = userId;
        proposal.DecidedAtUtc = DateTime.UtcNow;
        proposal.UpdatedAtUtc = DateTime.UtcNow;
        db.Set<AssistantActionAudit>().Add(new AssistantActionAudit
        {
            RestaurantId = restaurantId, ProposalId = proposal.Id, ActorUserId = userId,
            Action = "Approved", EntityType = proposal.EntityType, EntityId = proposal.EntityId,
            ValueBefore = proposal.OriginalValue, ValueAfter = proposal.ProposedValue,
            DetailsJson = proposal.EvidenceJson, OccurredAtUtc = DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(proposal, false, false, dish.Price);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return new(proposal, true, false, null);
        }
    }

    public async Task<AssistantProposal?> RejectAsync(int proposalId, int restaurantId, int userId, CancellationToken cancellationToken = default)
    {
        var proposal = await db.Set<AssistantProposal>().FirstOrDefaultAsync(p => p.Id == proposalId && p.RestaurantId == restaurantId, cancellationToken);
        if (proposal is null || proposal.Status != AssistantProposalStatus.Pending) return proposal;
        if (proposal.ExpiresAtUtc <= DateTime.UtcNow)
        {
            proposal.Status = AssistantProposalStatus.Expired;
            proposal.UpdatedAtUtc = DateTime.UtcNow;
            db.Set<AssistantActionAudit>().Add(new AssistantActionAudit
            {
                RestaurantId = restaurantId, ProposalId = proposal.Id, ActorUserId = 0,
                Action = "Expired", EntityType = proposal.EntityType, EntityId = proposal.EntityId,
                ValueBefore = proposal.OriginalValue, ValueAfter = proposal.OriginalValue,
                DetailsJson = proposal.EvidenceJson, OccurredAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            return proposal;
        }
        proposal.Status = AssistantProposalStatus.Rejected;
        proposal.DecidedByUserId = userId;
        proposal.DecidedAtUtc = DateTime.UtcNow;
        proposal.UpdatedAtUtc = DateTime.UtcNow;
        db.Set<AssistantActionAudit>().Add(new AssistantActionAudit
        {
            RestaurantId = restaurantId, ProposalId = proposal.Id, ActorUserId = userId,
            Action = "Rejected", EntityType = proposal.EntityType, EntityId = proposal.EntityId,
            ValueBefore = proposal.OriginalValue, ValueAfter = proposal.OriginalValue,
            DetailsJson = proposal.EvidenceJson, OccurredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return proposal;
    }

    public async Task<AssistantProposal?> ReviewAsync(int proposalId, int restaurantId, int userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var proposal = await db.Set<AssistantProposal>().FirstOrDefaultAsync(p => p.Id == proposalId && p.RestaurantId == restaurantId, cancellationToken);
        if (proposal is null || proposal.Status != AssistantProposalStatus.Pending) return proposal;

        var alert = await db.SystemAlerts.FirstOrDefaultAsync(a => a.Id == proposal.AlertId && a.RestaurantId == restaurantId, cancellationToken);
        if (alert is null) return null;
        alert.IsRead = true;
        alert.ReadAtUtc = DateTime.UtcNow;
        proposal.Status = AssistantProposalStatus.Approved;
        proposal.DecidedByUserId = userId;
        proposal.DecidedAtUtc = DateTime.UtcNow;
        proposal.UpdatedAtUtc = DateTime.UtcNow;
        db.Set<AssistantActionAudit>().Add(new AssistantActionAudit
        {
            RestaurantId = restaurantId, ProposalId = proposal.Id, ActorUserId = userId,
            Action = "Reviewed", EntityType = proposal.EntityType, EntityId = proposal.EntityId,
            DetailsJson = proposal.EvidenceJson, OccurredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return proposal;
    }
}
