using ISM.Domain.Modules.Assistant.Entities;

namespace ISM.Domain.Interfaces;

public interface IAssistantRepository
{
    Task<AssistantProposal?> GetProposalAsync(int id, int restaurantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssistantProposal>> GetProposalsAsync(int restaurantId, int page, int limit, CancellationToken cancellationToken = default);
    Task<AssistantProposal?> FindByAlertAsync(int alertId, int restaurantId, CancellationToken cancellationToken = default);
    Task<AssistantProposal> CreateProposalAsync(AssistantProposal proposal, CancellationToken cancellationToken = default);
    Task<AssistantApprovalResult> ApprovePriceAsync(int proposalId, int restaurantId, int userId, CancellationToken cancellationToken = default);
    Task<AssistantProposal?> RejectAsync(int proposalId, int restaurantId, int userId, CancellationToken cancellationToken = default);
    Task<AssistantProposal?> ReviewAsync(int proposalId, int restaurantId, int userId, CancellationToken cancellationToken = default);
}

public sealed record AssistantApprovalResult(AssistantProposal? Proposal, bool IsStale, bool WasAlreadyDecided, decimal? CurrentPrice);
