namespace ISM.Application.Interfaces.Assistant;

public interface IAutomationRunner
{
    Task<int> RunDueAsync(int restaurantId, CancellationToken cancellationToken = default);
}
