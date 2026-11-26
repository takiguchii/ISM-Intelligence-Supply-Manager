using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs.Restaurant;

public sealed class RestaurantAiPhotoConfigDto
{
    public bool HasCustomKey { get; set; }
    public string? KeyLast4Digits { get; set; }
    public string Provider { get; set; } = "Gemini";
    public string? ApiEndpoint { get; set; }
    public string? Model { get; set; }
    public bool IsUsingRestaurantConfig { get; set; }
    public bool IsUsingGlobalFallback { get; set; }
    public List<string>? Warnings { get; set; }
}

public sealed class UpdateRestaurantAiPhotoConfigDto
{
    [MaxLength(512)]
    public string? ApiKey { get; set; }

    [MaxLength(512)]
    public string? ApiEndpoint { get; set; }

    [MaxLength(40)]
    public string? Provider { get; set; }

    [MaxLength(128)]
    public string? Model { get; set; }

    public bool ClearKey { get; set; }
}

public sealed class TestRestaurantAiPhotoConfigRequestDto
{
    [MaxLength(512)]
    public string? OverrideApiKey { get; set; }

    [MaxLength(512)]
    public string? OverrideEndpoint { get; set; }

    [MaxLength(128)]
    public string? OverrideModel { get; set; }

    [MaxLength(40)]
    public string? OverrideProvider { get; set; }
}

public class TestRestaurantAiPhotoConfigResultDto
{
    public bool Success { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? NormalizedModel { get; set; }
    public string? NormalizedEndpoint { get; set; }
    public List<string>? Warnings { get; set; }
    public string? ErrorMessage { get; set; }
    public int? HttpStatusFromProvider { get; set; }
    public long? LatencyMs { get; set; }
}

public sealed class RestaurantAgentConfigDto
{
    public bool HasApiKey { get; set; }
    public string? KeyLast4Digits { get; set; }
    public string Provider { get; set; } = "OpenCodeGo";
    public string? ApiEndpoint { get; set; }
    public string? Model { get; set; }
}

public sealed class UpdateRestaurantAgentConfigDto
{
    [MaxLength(512)] public string? ApiKey { get; set; }
    [MaxLength(40)] public string? Provider { get; set; }
    [MaxLength(512)] public string? ApiEndpoint { get; set; }
    [MaxLength(128)] public string? Model { get; set; }
    public bool ClearKey { get; set; }
}

public sealed class TestRestaurantAgentConfigRequestDto
{
    [MaxLength(512)] public string? OverrideApiKey { get; set; }
    [MaxLength(512)] public string? OverrideEndpoint { get; set; }
    [MaxLength(128)] public string? OverrideModel { get; set; }
    [MaxLength(40)] public string? OverrideProvider { get; set; }
}

public sealed class TestRestaurantAgentConfigResultDto : TestRestaurantAiPhotoConfigResultDto { }
