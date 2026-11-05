namespace ISM.Domain.Entities;

public sealed class AuthSession
{
    public string Id { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Device { get; set; } = "Navegador";
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
