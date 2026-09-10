using ISM.Application.DTOs.Restaurant;

namespace ISM.Application.Interfaces.Restaurant;

public interface ITmpAuthorizationService
{
    Task<CreateTmpAuthorizationResult> CreateAsync(
        int restaurantId,
        int createdByUserId,
        CreateTmpAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TmpAuthorizationDto>> ListByRestaurantAsync(
        int restaurantId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        int tokenId,
        int restaurantId,
        int revokedByUserId,
        CancellationToken cancellationToken = default);

    Task<ValidateTmpAuthorizationResult> ValidateTokenAsync(
        string plainToken,
        string? requiredScope = null,
        CancellationToken cancellationToken = default);

    Task<TmpAuthorizationDto?> RotateAsync(
        int tokenId,
        int restaurantId,
        int rotatedByUserId,
        CancellationToken cancellationToken = default);
}
