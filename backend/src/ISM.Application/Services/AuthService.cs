using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ISM.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Tenta buscar por username primeiro, senão por email
        var user = await _userRepository.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken);
        if (user == null && request.UsernameOrEmail.Contains('@'))
        {
            user = await _userRepository.GetByEmailAsync(request.UsernameOrEmail, cancellationToken);
        }

        if (user == null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token = _tokenService.GenerateToken(user);
        return new LoginResponse(user.Username, user.Email, user.Role, token);
    }

    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Verifica se usuário já existe
        var existingUsername = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername != null)
        {
            throw new InvalidOperationException("Nome de usuário já está em uso.");
        }

        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            throw new InvalidOperationException("E-mail já está em uso.");
        }

        var role = request.Role;
        if (role != "Admin" && role != "User")
        {
            role = "User"; // Valor padrão seguro
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(user);
        return new LoginResponse(user.Username, user.Email, user.Role, token);
    }
}
