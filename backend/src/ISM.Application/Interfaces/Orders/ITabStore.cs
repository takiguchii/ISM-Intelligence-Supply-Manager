using ISM.Application.DTOs.Orders;

namespace ISM.Application.Interfaces.Orders;

public interface ITabStore
{
    Task<IReadOnlyList<OpenTabResponse>> GetOpenTabsAsync(int restaurantId, CancellationToken cancellationToken);
    Task<OpenTabResponse?> GetOpenAsync(int restaurantId, string tableReference, CancellationToken cancellationToken);
    Task<TabPaymentResponse?> PayAsync(int restaurantId, int tabId, PayTabRequest request, CancellationToken cancellationToken);
}

public sealed class TabPaymentConflictException(string message) : Exception(message);
