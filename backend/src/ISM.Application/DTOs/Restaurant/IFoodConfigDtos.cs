namespace ISM.Application.DTOs.Restaurant;

public sealed class IFoodConfigDto
{
    public bool HasCredentials { get; set; }
    public string? MerchantId { get; set; }
    public string? ClientIdLast4 { get; set; }
}

public sealed class UpdateIFoodConfigDto
{
    public string? ClientId  { get; set; }
    public string? ClientSecret { get; set; }
    public string? MerchantId { get; set; }
    public bool ClearCredentials { get; set; }
}

public sealed class TestIFoodConfigResultDto
{
    public bool Success { get; set; }
    public bool TokenObtained { get; set; }
    public string? ErrorMessage { get; set; }
    public long LatencyMs { get; set; }
}