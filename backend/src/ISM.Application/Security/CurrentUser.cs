namespace ISM.Application.Security;

public static class IsmRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Chef = "Chef";
    public const string Waiter = "Waiter";

    public static readonly IReadOnlyList<string> AllAllowed = new[] { Admin, Manager, Chef, Waiter };

    public static bool IsValid(string? role) =>
        !string.IsNullOrWhiteSpace(role) && AllAllowed.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? role) =>
        AllAllowed.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)) ?? Waiter;
}

public static class IsmPolicies
{
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string RestaurantManagerOrAbove = "RestaurantManagerOrAbove";
    public const string RestaurantAnyUser = "RestaurantAnyUser";
}

public interface ICurrentUser
{
    int UserId { get; }
    string? Email { get; }
    string Role { get; }
    int? RestaurantId { get; }
    bool IsSuperAdmin { get; }
    bool IsManagerOrAbove { get; }
}
