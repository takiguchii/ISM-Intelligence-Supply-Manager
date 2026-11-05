using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Domain.Entities;

public static class NotificationPolicy
{
    public static bool CanReceive(User user, SystemAlert alert) => user.IsActive &&
        user.RestaurantId == alert.RestaurantId && user.Role is "Admin" or "Manager" &&
        (alert.AlertType switch
        {
            SystemAlertType.StockRuptureRisk48h or SystemAlertType.StockBelowMinimum => user.NotifyStock,
            SystemAlertType.SupplierPriceSpike15Percent or SystemAlertType.SupplierBetterAlternative => user.NotifySuppliers,
            SystemAlertType.DishCmvDefasagem => user.NotifyMenu,
            _ => false
        });
}
