namespace ISM.Application.DTOs;

public sealed class TmpAuthorizationDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public int? RevokedByUserId { get; set; }
    public string? RevokedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AutoRotateAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? PlainTokenPreview { get; set; }
}

public sealed class CreateTmpAuthorizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = "read";
    public string? Description { get; set; }
    public int ValidityHours { get; set; } = 720;
    public int? AutoRotateHours { get; set; }
}

public sealed class CreateTmpAuthorizationResult
{
    public TmpAuthorizationDto Token { get; set; } = default!;
    public string PlainToken { get; set; } = string.Empty;
}

public sealed class ValidateTmpAuthorizationResult
{
    public bool IsValid { get; set; }
    public string? FailReason { get; set; }
    public int RestaurantId { get; set; }
    public int? TokenId { get; set; }
    public string? Scope { get; set; }
}
