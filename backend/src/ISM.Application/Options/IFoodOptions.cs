namespace ISM.Application.Options;

public sealed class IFoodOptions
{
    public const string SectionName = "IFood";
    public string AuthBaseUrl { get; set; } = "https://merchant-api.ifood.com.br/authentication/v1.0";
    public string ApiBaseUrl { get; set; } = "https://merchant-api.ifood.com.br";
    public int TokenCacheSafetyMarginSeconds { get; set; } = 60;
}