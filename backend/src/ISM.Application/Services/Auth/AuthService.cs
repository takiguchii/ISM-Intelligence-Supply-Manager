using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OtpNet;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ISM.Application.Services.Auth;

public sealed class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Pbkdf2Iterations = 100_000;
    private const byte FormatVersion = 0x01;
    private readonly ITwoFactorProtector _twoFactorProtector;
    private readonly IAuthSessionRepository _sessions;
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
        IPlanEnforcer planEnforcer,
        IAuthSessionRepository sessions,
        ITwoFactorProtector twoFactorProtector)
    {
        _twoFactorProtector = twoFactorProtector;
        _sessions = sessions;
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

        var (isPlanActive, planMessage) = await _planEnforcer.ValidateRestaurantAccessAsync(user.RestaurantId, cancellationToken);
        if (!isPlanActive)
            throw new UnauthorizedAccessException(planMessage ?? "Acesso bloqueado.");

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode)) throw new TwoFactorRequiredException();
            if (!ConsumeSecondFactor(user, request.TwoFactorCode))
                throw new UnauthorizedAccessException("Código de autenticação inválido ou já utilizado.");
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("E-mail já cadastrado.");

        if (!IsmRoles.IsValid(request.Role))
            throw new InvalidOperationException($"Role inválido '{request.Role}'.");

        var requestedRole = IsmRoles.Normalize(request.Role);

        // 🔒 PONTO 4: Manager logado NÃO PODE criar Admin nem SuperAdmin
        if (!_currentUser.IsSuperAdmin)
        {
            if (!IsTenantManager() ||
                !_currentUser.RestaurantId.HasValue)
                throw new UnauthorizedAccessException("Apenas administradores ou gerentes do restaurante podem criar usuários.");

            // 🔒 Manager só pode criar usuário PROPRIO RESTAURANTE
            if (_currentUser.RestaurantId.HasValue)
            {
                if (!request.RestaurantId.HasValue || request.RestaurantId.Value != _currentUser.RestaurantId.Value)
                    throw new UnauthorizedAccessException("Você só pode criar usuários no seu próprio restaurante.");

            }

            // Limite de plano (PONTO 5): só se for criar user em restaurante
            if (request.RestaurantId.HasValue)
                await _planEnforcer.AssertCanAddUserAsync(request.RestaurantId.Value, cancellationToken);

            if (!CanManageRole(requestedRole))
                throw new UnauthorizedAccessException("Você não pode criar usuários com este cargo.");
        }

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
            Role = requestedRole,
            IsActive = true,
            RestaurantId = request.RestaurantId,
            CreatedAtUtc = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<RegisterTenantResponse> RegisterTenantAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Manager.Email, cancellationToken))
            throw new InvalidOperationException("E-mail do gerente já cadastrado no sistema.");

        var restaurantDto = await _restaurantService.CreateRestaurantAsync(
            new RestaurantDto { Name = request.Restaurant.Name, CNPJ = request.Restaurant.Cnpj },
            request.PlanId,
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

        var auth = await CreateSessionAsync(user, cancellationToken);
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
        if (!user.IsActive || user.TwoFactorEnabled)
            throw new InvalidOperationException("Use o login normal para esta conta.");
        return CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> GenerateAdminTokenAsync(CancellationToken cancellationToken = default)
    {
        var admin = await _userRepository.GetByIdAsync(1, cancellationToken);
        if (admin == null)
            throw new InvalidOperationException("Usuário admin padrão (ID=1) não encontrado no banco.");

        if (!admin.IsActive || admin.TwoFactorEnabled)
            throw new InvalidOperationException("Use o login normal para esta conta.");
        return await CreateSessionAsync(admin, cancellationToken);
    }

    public async Task<bool> TwoFactorEnabledAsync(CancellationToken ct = default) =>
        (await _userRepository.GetByIdAsync(_currentUser.UserId, ct))?.TwoFactorEnabled ?? false;

    public async Task<TwoFactorSetup> SetupTwoFactorAsync(string password, CancellationToken ct = default)
    {
        var user = await RequirePasswordAsync(password, ct);
        if (user.TwoFactorEnabled) throw new InvalidOperationException("A autenticação em duas etapas já está ativa.");
        var secret = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        user.TwoFactorSecret = _twoFactorProtector.Protect(secret);
        user.TwoFactorSetupExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(ct);
        var label = Uri.EscapeDataString("ISM:" + user.Email);
        return new TwoFactorSetup(secret, $"otpauth://totp/{label}?secret={secret}&issuer=ISM&algorithm=SHA1&digits=6&period=30");
    }

    public async Task<string[]> EnableTwoFactorAsync(string code, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, ct);
        if (user == null || user.TwoFactorEnabled || user.TwoFactorSetupExpiresAtUtc == null ||
            user.TwoFactorSetupExpiresAtUtc <= DateTime.UtcNow || !VerifyTotp(user, code))
            throw new InvalidOperationException("Código inválido ou configuração expirada. Inicie novamente.");
        var codes = Enumerable.Range(0, 8).Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(8))).ToArray();
        user.RecoveryCodeHashes = JsonSerializer.Serialize(codes.Select(HashRecoveryCode));
        user.TwoFactorEnabled = true;
        user.TwoFactorSetupExpiresAtUtc = null;
        user.PasswordChangedAtUtc = DateTime.UtcNow; // invalida tokens anteriores ao registro de sessões
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(ct);
        await _sessions.RevokeAsync(user.Id, null, ct);
        return codes;
    }

    public async Task DisableTwoFactorAsync(string password, string code, CancellationToken ct = default)
    {
        var user = await RequirePasswordAsync(password, ct);
        if (!user.TwoFactorEnabled || !ConsumeSecondFactor(user, code))
            throw new InvalidOperationException("Código de autenticação inválido ou já utilizado.");
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.LastTotpStep = null;
        user.RecoveryCodeHashes = "[]";
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(ct);
    }

    private async Task<User> RequirePasswordAsync(string password, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, ct);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
            throw new InvalidOperationException("Senha atual incorreta.");
        return user;
    }

    private bool VerifyTotp(User user, string code)
    {
        if (user.TwoFactorSecret == null || code.Length != 6 || !code.All(char.IsAsciiDigit)) return false;
        var key = Base32Encoding.ToBytes(_twoFactorProtector.Unprotect(user.TwoFactorSecret));
        if (!new Totp(key).VerifyTotp(code, out var step, new VerificationWindow(previous: 1, future: 1)) ||
            (user.LastTotpStep.HasValue && step <= user.LastTotpStep.Value)) return false;
        user.LastTotpStep = step;
        return true;
    }

    private bool ConsumeSecondFactor(User user, string code)
    {
        if (VerifyTotp(user, code)) return true;
        var hashes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodeHashes) ?? [];
        if (!hashes.Remove(HashRecoveryCode(code))) return false;
        user.RecoveryCodeHashes = JsonSerializer.Serialize(hashes);
        return true;
    }

    private static string HashRecoveryCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant())));

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        if (user == null || !VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Senha atual incorreta.");
        if (request.NewPassword.Length < 8 || request.NewPassword.Length > 128)
            throw new InvalidOperationException("A nova senha deve ter de 8 a 128 caracteres.");
        if (VerifyPassword(request.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("A nova senha deve ser diferente da atual.");
        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _sessions.RevokeAsync(user.Id, null, cancellationToken);
    }

    private async Task<AuthResponse> CreateSessionAsync(User user, CancellationToken ct)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresInMinutes);
        var sessionId = Guid.NewGuid().ToString();
        await _sessions.AddAsync(new AuthSession
        {
            Id = sessionId, UserId = user.Id, CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = expires
        }, ct);
        var token = GenerateJwtToken(user, expires, sessionId);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expires,
            User = MapToUserDto(user)
        };
    }

    private bool IsTenantManager() =>
        _currentUser.Role is IsmRoles.Admin or IsmRoles.Manager && _currentUser.RestaurantId.HasValue;

    private bool CanManageRole(string role) =>
        _currentUser.Role == IsmRoles.Admin
            ? role is IsmRoles.Manager or IsmRoles.Chef or IsmRoles.Waiter
            : role is IsmRoles.Chef or IsmRoles.Waiter;

    private string GenerateJwtToken(User user, DateTime expires, string sessionId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("restaurantId", user.RestaurantId?.ToString() ?? string.Empty),
            new Claim("sid", sessionId),
            new Claim(JwtRegisteredClaimNames.Jti, sessionId)
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
