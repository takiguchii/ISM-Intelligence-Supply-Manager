using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDetailsDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDetailsDto>> GetUsersByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<UserDetailsDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDetailsDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailsDto> ToggleUserActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}
