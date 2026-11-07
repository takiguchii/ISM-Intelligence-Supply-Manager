namespace ISM.Domain.Entities;

public sealed class NotificationDelivery
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int RestaurantId { get; set; }
    public ISM.Domain.Modules.System.Entities.SystemAlert? Alert { get; set; }
    public int? AlertId { get; set; }
    public string Channel { get; set; } = "email";
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? Scenario { get; set; }
    public bool IsTest { get; set; }
    public int? SourceAlertId { get; set; }
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
}
