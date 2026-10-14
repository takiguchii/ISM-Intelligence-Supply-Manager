using ISM.Application.Common;

namespace ISM.Application.Interfaces.Menu;

public interface IPricingAgent
{
    Task<AgentRunSummary> RunAsync(
        int? restaurantIdFilter = null,
        CancellationToken cancellationToken = default);
}
