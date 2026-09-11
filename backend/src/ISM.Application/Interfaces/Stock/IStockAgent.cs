using ISM.Application.Common;

namespace ISM.Application.Interfaces.Stock;

public interface IStockAgent
{
    Task<AgentRunSummary> RunAsync(
        int? restaurantIdFilter = null,
        CancellationToken cancellationToken = default);
}
