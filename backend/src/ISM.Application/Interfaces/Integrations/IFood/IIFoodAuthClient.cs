namespace ISM.Application.Interfaces.Integrations.IFood;

public interface IIFoodAuthClient
{
    Task<string> GetAccessTokenAsync(int restaurantId, CancellationToken ct);
    void InvalidateCachedToken(int restaurantId);
}