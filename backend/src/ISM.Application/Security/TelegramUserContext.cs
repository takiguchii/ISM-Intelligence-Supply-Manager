namespace ISM.Application.Security;

// Identidade de uma única mensagem, carregada do banco, nunca de campos do Telegram.
public sealed class TelegramUserContext
{
    public int UserId { get; private set; }
    public int? RestaurantId { get; private set; }
    public string Role { get; private set; } = "";
    public void Set(int userId, int restaurantId, string role)
        => (UserId, RestaurantId, Role) = (userId, restaurantId, role);
}
