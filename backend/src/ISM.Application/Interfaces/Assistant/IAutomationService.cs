using ISM.Application.DTOs.Assistant;

namespace ISM.Application.Interfaces.Assistant;

public interface IAutomationService
{
    Task<IReadOnlyList<AutomationPolicyResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<AutomationPolicyResponse?> CreateAsync(CreateAutomationPolicyRequest request, CancellationToken cancellationToken = default);
    Task<(AutomationPolicyResponse? Policy, int StatusCode)> ConfirmAsync(int id, ConfirmAutomationRequest request, CancellationToken cancellationToken = default);
    Task<AutomationPolicyResponse?> PauseAsync(int id, CancellationToken cancellationToken = default);
    Task<AutomationPolicyResponse?> RevokeAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutomationRunResponse>> RunsAsync(int id, CancellationToken cancellationToken = default);
}
