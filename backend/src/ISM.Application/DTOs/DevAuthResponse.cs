using System;

namespace ISM.Application.DTOs;

public sealed class DevAuthResponse
{
    public string Source { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
