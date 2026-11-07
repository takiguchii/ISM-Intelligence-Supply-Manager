namespace ISM.Domain.Entities;

public sealed class User : BaseEntity
{
    public DateTime? PasswordChangedAtUtc { get; set; }
    public string AppearancePreferencesJson { get; set; } = "{}";
    public string? NotificationEmailAddress { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool WhatsAppNotificationsEnabled { get; set; }
    public string? WhatsAppPhone { get; set; }
    public bool NotifyStock { get; set; } = true;
    public bool NotifySuppliers { get; set; } = true;
    public bool NotifyMenu { get; set; } = true;
    public string? TwoFactorSecret { get; set; }
    public DateTime? TwoFactorSetupExpiresAtUtc { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public long? LastTotpStep { get; set; }
    public string RecoveryCodeHashes { get; set; } = "[]";
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public bool IsActive { get; set; } = true;
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
}
