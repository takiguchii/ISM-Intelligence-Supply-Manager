using ISM.Application.DTOs;
using ISM.Domain.Entities;

namespace ISM.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AuthResponse> GenerateTokenForUserAsync(User user, CancellationToken cancellationToken = default);
    Task<AuthResponse> GenerateAdminTokenAsync(CancellationToken cancellationToken = default);
}
