namespace ISM.Application.Options;

public sealed class PhotoImportOptions
{
    public const string SectionName = "PhotoImport";

    /// <summary>Provedor de visão. Atualmente: "OpenAICompatible".</summary>
    public string Provider { get; set; } = "OpenAICompatible";

    /// <summary>Endpoint base compatível com OpenAI (ex: https://api.openai.com/v1).</summary>
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>Modelo com suporte a visão (ex: gpt-4o-mini).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Não colocar a chave no appsettings em produção: usar env var PHOTO_IMPORT__APIKEY.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Tamanho máximo do arquivo em MB.</summary>
    public int MaxFileSizeMb { get; set; } = 20;
}
