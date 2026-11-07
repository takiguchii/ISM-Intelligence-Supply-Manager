using ISM.Application.DTOs;
using ISM.Domain.Entities;

namespace ISM.Application.Interfaces;

public interface IAuthService
{
    Task<bool> TwoFactorEnabledAsync(CancellationToken ct = default);
    Task<TwoFactorSetup> SetupTwoFactorAsync(string password, CancellationToken ct = default);
    Task<string[]> EnableTwoFactorAsync(string code, CancellationToken ct = default);
    Task DisableTwoFactorAsync(string password, string code, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<RegisterTenantResponse> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AuthResponse> GenerateTokenForUserAsync(User user, CancellationToken cancellationToken = default);
    Task<AuthResponse> GenerateAdminTokenAsync(CancellationToken cancellationToken = default);
}
