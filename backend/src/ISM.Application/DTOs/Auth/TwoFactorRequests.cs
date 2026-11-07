using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs;

public sealed class TwoFactorPasswordRequest
{
    [Required, StringLength(256)]
    public string Password { get; set; } = string.Empty;
}

public sealed class TwoFactorCodeRequest
{
    [Required, StringLength(32)]
    public string Code { get; set; } = string.Empty;
}

public sealed class DisableTwoFactorRequest
{
    [Required, StringLength(256)]
    public string Password { get; set; } = string.Empty;
    [Required, StringLength(32)]
    public string Code { get; set; } = string.Empty;
}

public sealed record TwoFactorSetup(string Secret, string ProvisioningUri);
public sealed class TwoFactorRequiredException : Exception;
