namespace ISM.Application.Services.Notifications;

public static class NotificationLinkService
{
    // Domínio público do ISM, mantido no código para dispensar configuração no servidor.
    public const string PublicAppUrl = "https://ism.altvia.cloud";

    public static Uri CreateActionUri(string path, string baseUrl = PublicAppUrl)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + path, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Configure um endereço HTTP ou HTTPS válido para os links do ISM.");
        return uri;
    }
}
