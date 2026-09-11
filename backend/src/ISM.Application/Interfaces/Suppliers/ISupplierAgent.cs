using ISM.Application.Common;

namespace ISM.Application.Interfaces.Suppliers;

public interface ISupplierAgent
{
    Task<AgentRunSummary> RunAsync(
        int? restaurantIdFilter = null,
        CancellationToken cancellationToken = default);
}
