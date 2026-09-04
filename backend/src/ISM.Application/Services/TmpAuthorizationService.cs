using System.Security.Cryptography;
using System.Text;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services;

public sealed class TmpAuthorizationService : ITmpAuthorizationService
{
    private readonly ITmpAuthorizationRepository _repository;

    public TmpAuthorizationService(ITmpAuthorizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateTmpAuthorizationResult> CreateAsync(
        int restaurantId,
        int createdByUserId,
        CreateTmpAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Nome do token é obrigatório.", nameof(request));
        if (request.ValidityHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.ValidityHours), "Validade deve ser maior que zero.");

        var plainToken = GeneratePlainToken();
        var tokenHash = HashToken(plainToken);

        var entity = new TmpAuthorization
        {
            RestaurantId = restaurantId,
            Name = request.Name.Trim(),
            TokenHash = tokenHash,
            Scope = string.IsNullOrWhiteSpace(request.Scope) ? "read" : request.Scope.Trim().ToLowerInvariant(),
            CreatedByUserId = createdByUserId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(request.ValidityHours),
            AutoRotateAtUtc = request.AutoRotateHours.HasValue && request.AutoRotateHours.Value > 0
                ? DateTime.UtcNow.AddHours(request.AutoRotateHours.Value)
                : null,
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);
        var dto = MapToDto(created, plainTokenPreview: $"ism_{plainToken.AsSpan(0, 8)}...");

        return new CreateTmpAuthorizationResult
        {
            Token = dto,
            PlainToken = $"ism_{plainToken}"
        };
    }

    public async Task<IReadOnlyList<TmpAuthorizationDto>> ListByRestaurantAsync(
        int restaurantId,
        CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListByRestaurantAsync(restaurantId, cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<bool> RevokeAsync(
        int tokenId,
        int restaurantId,
        int revokedByUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (entity == null || entity.RestaurantId != restaurantId) return false;
        if (entity.RevokedAtUtc.HasValue) return true;

        entity.RevokedAtUtc = DateTime.UtcNow;
        entity.RevokedByUserId = revokedByUserId;
        return await _repository.UpdateAsync(entity, cancellationToken);
    }

    public async Task<ValidateTmpAuthorizationResult> ValidateTokenAsync(
        string plainToken,
        string? requiredScope = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
            return new ValidateTmpAuthorizationResult { IsValid = false, FailReason = "Token vazio." };

        var cleanToken = plainToken.StartsWith("ism_", StringComparison.OrdinalIgnoreCase)
            ? plainToken.Substring(4)
            : plainToken;
        var hash = HashToken(cleanToken);

        var entity = await _repository.GetByTokenHashAsync(hash, cancellationToken);
        if (entity == null)
            return new ValidateTmpAuthorizationResult { IsValid = false, FailReason = "Token não encontrado." };
        if (entity.RevokedAtUtc.HasValue)
            return new ValidateTmpAuthorizationResult { IsValid = false, FailReason = "Token revogado." };
        if (entity.ExpiresAtUtc <= DateTime.UtcNow)
            return new ValidateTmpAuthorizationResult { IsValid = false, FailReason = "Token expirado." };
        if (!string.IsNullOrWhiteSpace(requiredScope) &&
            !entity.Scope.Equals(requiredScope, StringComparison.OrdinalIgnoreCase) &&
            !entity.Scope.Equals("full", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidateTmpAuthorizationResult { IsValid = false, FailReason = $"Escopo insuficiente (requer {requiredScope})." };
        }

        entity.LastUsedAtUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(entity, cancellationToken);

        return new ValidateTmpAuthorizationResult
        {
            IsValid = true,
            RestaurantId = entity.RestaurantId,
            TokenId = entity.Id,
            Scope = entity.Scope
        };
    }

    public async Task<TmpAuthorizationDto?> RotateAsync(
        int tokenId,
        int restaurantId,
        int rotatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetByIdAsync(tokenId, cancellationToken);
        if (current == null || current.RestaurantId != restaurantId) return null;
        if (current.RevokedAtUtc.HasValue) return null;

        var novoPlain = GeneratePlainToken();
        var novoHash = HashToken(novoPlain);

        var horasRestantes = Math.Max(1, (int)Math.Ceiling((current.ExpiresAtUtc - DateTime.UtcNow).TotalHours));
        var scopeOrigem = current.Scope;
        var nomeOrigem = current.Name;
        var descOrigem = current.Description;
        var autoRotateOrigem = current.AutoRotateAtUtc.HasValue
            ? (int?)Math.Max(1, (int)Math.Ceiling(current.AutoRotateAtUtc.Value.Subtract(DateTime.UtcNow).TotalHours))
            : null;

        var request = new CreateTmpAuthorizationRequest
        {
            Name = $"{nomeOrigem} (rotacionado {DateTime.UtcNow:yyyyMMddHHmm})",
            Scope = scopeOrigem,
            Description = descOrigem,
            ValidityHours = horasRestantes,
            AutoRotateHours = autoRotateOrigem
        };

        var revogado = await RevokeAsync(tokenId, restaurantId, rotatedByUserId, cancellationToken);
        if (!revogado) return null;

        var novo = await CreateAsync(restaurantId, rotatedByUserId, request, cancellationToken);
        return novo.Token;
    }

    private static TmpAuthorizationDto MapToDto(TmpAuthorization entity, string? plainTokenPreview = null)
    {
        return new TmpAuthorizationDto
        {
            Id = entity.Id,
            RestaurantId = entity.RestaurantId,
            Name = entity.Name,
            Scope = entity.Scope,
            Description = entity.Description,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedByUserName = entity.CreatedByUser?.Name,
            RevokedByUserId = entity.RevokedByUserId,
            RevokedByUserName = entity.RevokedByUser?.Name,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            AutoRotateAtUtc = entity.AutoRotateAtUtc,
            RevokedAtUtc = entity.RevokedAtUtc,
            LastUsedAtUtc = entity.LastUsedAtUtc,
            IsActive = entity.IsActive,
            PlainTokenPreview = plainTokenPreview
        };
    }

    private static string GeneratePlainToken()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        var data = new byte[48];
        rng.GetBytes(data);
        var sb = new StringBuilder(48);
        foreach (var b in data) sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }

    private static string HashToken(string plainToken)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainToken));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
