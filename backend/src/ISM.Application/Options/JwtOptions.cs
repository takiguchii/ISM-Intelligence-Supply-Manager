namespace ISM.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ISM";
    public string Audience { get; set; } = "ISM.Users";
    public string SecretKey { get; set; } = "SUPER_SECRET_KEY_FOR_DEVELOPMENT_ONLY_CHANGE_IN_PRODUCTION_PLEASE_123456";
    public int ExpiresInMinutes { get; set; } = 1440;
}
