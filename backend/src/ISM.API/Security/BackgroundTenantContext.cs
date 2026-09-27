using ISM.Application.Security;

namespace ISM.API.Security;

public sealed class BackgroundTenantContext : IBackgroundTenantContext
{
    public int? RestaurantId { get; private set; }

    public void SetRestaurant(int restaurantId)
    {
        if (restaurantId <= 0) throw new ArgumentOutOfRangeException(nameof(restaurantId));
        RestaurantId = restaurantId;
    }

    public void Clear() => RestaurantId = null;
}
