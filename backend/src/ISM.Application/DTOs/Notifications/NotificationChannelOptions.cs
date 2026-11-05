namespace ISM.Application.DTOs.Notifications;

public sealed class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Security { get; set; } = "StartTls";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "ISM";
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(Host) &&
        Port is > 0 and <= 65535 && !string.IsNullOrWhiteSpace(FromAddress) &&
        Security is "StartTls" or "SslOnConnect" or "None" &&
        (string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password));
}

public sealed class WhatsAppOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://whatsapp:3000";
    public string ApiKey { get; set; } = string.Empty;
    public string Session { get; set; } = "default";
    public bool IsConfigured => Enabled && Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Session);
}
