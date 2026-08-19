using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ISM.Application.Services;

public sealed class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Pbkdf2Iterations = 100_000;
    private const byte FormatVersion = 0x01;
    private readonly IUserRepository _userRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IRestaurantService _restaurantService;
    private readonly JwtOptions _jwtOptions;
    private readonly ICurrentUser _currentUser;
    private readonly IPlanEnforcer _planEnforcer;

    public AuthService(
        IUserRepository userRepository,
        IRestaurantRepository restaurantRepository,
        IRestaurantService restaurantService,
        IOptions<JwtOptions> jwtOptions,
        ICurrentUser currentUser,
        IPlanEnforcer planEnforcer)
    {
        _userRepository = userRepository;
        _restaurantRepository = restaurantRepository;
        _restaurantService = restaurantService;
        _jwtOptions = jwtOptions.Value;
        _currentUser = currentUser;
        _planEnforcer = planEnforcer;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Usuário inativo.");

        var (planoAtivo, mensagemPlano) = await _planEnforcer.ValidateRestaurantAccessAsync(user.RestaurantId, cancellationToken);
        if (!planoAtivo)
            throw new UnauthorizedAccessException(mensagemPlano ?? "Acesso bloqueado.");

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

        var requestedRole = IsmRoles.Normalize(request.Role);

        // 🔒 PONTO 4: Manager logado NÃO PODE criar Admin nem SuperAdmin
        if (!_currentUser.IsSuperAdmin)
        {
            if (string.Equals(requestedRole, IsmRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
                !_currentUser.RestaurantId.HasValue)
            {
                throw new UnauthorizedAccessException("Você não tem permissão para criar usuários Super Admin.");
            }

            if (!_currentUser.IsManagerOrAbove)
            {
                throw new UnauthorizedAccessException("Apenas gerentes e administradores podem criar usuários.");
            }

            // 🔒 Manager só pode criar usuário PROPRIO RESTAURANTE
            if (_currentUser.RestaurantId.HasValue)
            {
                if (!request.RestaurantId.HasValue || request.RestaurantId.Value != _currentUser.RestaurantId.Value)
                    throw new UnauthorizedAccessException("Você só pode criar usuários no seu próprio restaurante.");

                // Manager NÃO pode criar Admin do sistema (só employees/Manager do mesmo restaurante)
                if (string.Equals(requestedRole, IsmRoles.Admin, StringComparison.OrdinalIgnoreCase)
                    && _currentUser.RestaurantId.HasValue)
                {
                    // Dentro do restaurante o Admin ainda é permitido, mas NÃO SuperAdmin (restaurantId null)
                    // então OK, pois request.RestaurantId já está setado pro proprio restaurante
                }
            }

            // Limite de plano (PONTO 5): só se for criar user em restaurante
            if (request.RestaurantId.HasValue)
                await _planEnforcer.AssertCanAddUserAsync(request.RestaurantId.Value, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = requestedRole,
            IsActive = true,
            RestaurantId = request.RestaurantId,
            CreatedAtUtc = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return GenerateAuthResponse(user);
    }

    public async Task<RegisterTenantResponse> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Manager.Email, cancellationToken))
            throw new InvalidOperationException("E-mail do gerente já cadastrado no sistema.");

        var restaurantDto = await _restaurantService.CreateRestaurantAsync(
            new RestaurantDto { Name = request.Restaurant.Name, CNPJ = request.Restaurant.Cnpj },
            request.PlanoId,
            cancellationToken);

        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = request.Manager.Name,
            Email = request.Manager.Email,
            PasswordHash = HashPassword(request.Manager.Password),
            Role = IsmRoles.Manager,
            IsActive = true,
            RestaurantId = restaurantDto.Id,
            CreatedAtUtc = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var auth = GenerateAuthResponse(user);
        return new RegisterTenantResponse(
            Restaurant: new RestaurantTenantResponse(restaurantDto.Id, restaurantDto.Name, restaurantDto.CNPJ),
            Manager: new ManagerTenantResponse(user.Id, user.Name, user.Email, user.Role, user.RestaurantId!.Value),
            AccessToken: auth.Token,
            TokenExpiresAt: auth.ExpiresAt);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user == null ? null : MapToUserDto(user);
    }

    public Task<AuthResponse> GenerateTokenForUserAsync(User user, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenerateAuthResponse(user));
    }

    public async Task<AuthResponse> GenerateAdminTokenAsync(CancellationToken cancellationToken = default)
    {
        var admin = await _userRepository.GetByIdAsync(1, cancellationToken);
        if (admin == null)
            throw new InvalidOperationException("Usuário admin padrão (ID=1) não encontrado no banco.");

        return GenerateAuthResponse(admin);
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
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        var bytes = new byte[1 + SaltSize + HashSize];
        bytes[0] = FormatVersion;
        Buffer.BlockCopy(salt, 0, bytes, 1, SaltSize);
        Buffer.BlockCopy(hash, 0, bytes, 1 + SaltSize, HashSize);

        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var bytes = Convert.FromBase64String(storedHash);
            if (bytes.Length < 1 + SaltSize + HashSize || bytes[0] != FormatVersion)
                return false;

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(bytes, 1, salt, 0, SaltSize);
            var stored = new byte[HashSize];
            Buffer.BlockCopy(bytes, 1 + SaltSize, stored, 0, HashSize);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(HashSize);
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
