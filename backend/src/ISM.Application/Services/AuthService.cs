using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Options;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ISM.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IUserRepository userRepository,
        IRestaurantRepository restaurantRepository,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _restaurantRepository = restaurantRepository;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Usuário inativo.");

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("E-mail já cadastrado.");

        if (request.RestaurantId.HasValue)
        {
            var restaurantExists = await _restaurantRepository.GetRestaurantByIdAsync(request.RestaurantId.Value, cancellationToken);
            if (restaurantExists == null)
                throw new InvalidOperationException($"Restaurante com ID {request.RestaurantId.Value} não existe.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Admin" : request.Role,
            IsActive = true,
            RestaurantId = request.RestaurantId,
            CreatedAtUtc = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return GenerateAuthResponse(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user == null ? null : MapToUserDto(user);
    }

    private AuthResponse GenerateAuthResponse(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresInMinutes);
        var token = GenerateJwtToken(user, expires);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expires,
            User = MapToUserDto(user)
        };
    }

    private string GenerateJwtToken(User user, DateTime expires)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("restaurantId", user.RestaurantId?.ToString() ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, 16, 100_000, HashAlgorithmName.SHA256);
        var salt = pbkdf2.Salt;
        var hash = pbkdf2.GetBytes(32);

        var bytes = new byte[1 + salt.Length + hash.Length];
        bytes[0] = 0x01;
        Buffer.BlockCopy(salt, 0, bytes, 1, salt.Length);
        Buffer.BlockCopy(hash, 0, bytes, 1 + salt.Length, hash.Length);

        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var bytes = Convert.FromBase64String(storedHash);
            if (bytes.Length < 49 || bytes[0] != 0x01)
                return false;

            var salt = new byte[16];
            Buffer.BlockCopy(bytes, 1, salt, 0, 16);
            var stored = new byte[32];
            Buffer.BlockCopy(bytes, 17, stored, 0, 32);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(stored, computed);
        }
        catch
        {
            return false;
        }
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        RestaurantId = user.RestaurantId
    };
}
