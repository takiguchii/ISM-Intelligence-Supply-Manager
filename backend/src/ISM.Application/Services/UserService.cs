using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRestaurantRepository _restaurantRepository;

    public UserService(IUserRepository userRepository, IRestaurantRepository restaurantRepository)
    {
        _userRepository = userRepository;
        _restaurantRepository = restaurantRepository;
    }

    public async Task<IReadOnlyList<UserDetailsDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToDetailsDto).ToList();
    }

    public async Task<IReadOnlyList<UserDetailsDto>> GetUsersByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, cancellationToken);
        if (restaurant == null)
            throw new InvalidOperationException($"Restaurante com ID {restaurantId} não existe.");

        var users = await _userRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
        return users.Select(MapToDetailsDto).ToList();
    }

    public async Task<UserDetailsDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user == null ? null : MapToDetailsDto(user);
    }

    public async Task<UserDetailsDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Usuário com ID {id} não existe.");

        if (request.RestaurantId.HasValue)
        {
            var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(request.RestaurantId.Value, cancellationToken);
            if (restaurant == null)
                throw new InvalidOperationException($"Restaurante com ID {request.RestaurantId.Value} não existe.");
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
            if (emailExists)
                throw new InvalidOperationException("E-mail já cadastrado para outro usuário.");
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.Role = request.Role;
        user.RestaurantId = request.RestaurantId;
        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return MapToDetailsDto(user);
    }

    public async Task<UserDetailsDto> ToggleUserActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Usuário com ID {id} não existe.");

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return MapToDetailsDto(user);
    }

    public async Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) return false;

        _userRepository.Delete(user);
        return await _userRepository.SaveChangesAsync(cancellationToken);
    }

    private static UserDetailsDto MapToDetailsDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        RestaurantId = user.RestaurantId,
        IsActive = user.IsActive,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc
    };
}
