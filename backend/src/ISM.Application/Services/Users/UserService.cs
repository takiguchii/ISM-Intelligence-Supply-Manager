using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUser _currentUser;

    public UserService(
        IUserRepository userRepository,
        IRestaurantRepository restaurantRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _restaurantRepository = restaurantRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserDetailsDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        if (!_currentUser.IsSuperAdmin)
        {
            // Global filter ja aplicou, mas garantimos aqui
            if (_currentUser.RestaurantId.HasValue)
                users = users.Where(u => u.RestaurantId == _currentUser.RestaurantId.Value).ToList();
        }
        return users.Select(MapToDetailsDto).ToList();
    }

    public async Task<IReadOnlyList<UserDetailsDto>> GetUsersByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsSuperAdmin)
        {
            if (!_currentUser.RestaurantId.HasValue || _currentUser.RestaurantId.Value != restaurantId)
                throw new UnauthorizedAccessException("Você não tem permissão para listar usuários de outro restaurante.");
        }

        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, cancellationToken);
        if (restaurant == null)
            throw new InvalidOperationException($"Restaurante com ID {restaurantId} não existe.");

        var users = await _userRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
        return users.Select(MapToDetailsDto).ToList();
    }

    public async Task<UserDetailsDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (user.RestaurantId != _currentUser.RestaurantId.Value)
                return null; // retorna 404 como se não existisse (não existe na tenant do usuario logado)
        }

        return MapToDetailsDto(user);
    }

    public async Task<UserDetailsDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Usuário com ID {id} não existe.");

        if (!_currentUser.IsSuperAdmin)
        {
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem editar usuários.");

            // Garantir que esta editando user do SEU restaurante
            if (_currentUser.RestaurantId.HasValue)
            {
                if (user.RestaurantId != _currentUser.RestaurantId.Value)
                    throw new UnauthorizedAccessException("Você só pode editar usuários do seu próprio restaurante.");

                // Nao pode mover user para outro restaurante
                if (request.RestaurantId.HasValue && request.RestaurantId.Value != _currentUser.RestaurantId.Value)
                    throw new UnauthorizedAccessException("Você não pode alterar o restaurante de um usuário.");

                // Nao pode promover ninguem a SuperAdmin (RestaurantId null + Admin)
                if (string.Equals(request.Role, IsmRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
                    !request.RestaurantId.HasValue)
                    throw new UnauthorizedAccessException("Você não tem permissão para criar Super Admin.");

                // Nao pode dar ROLE acima da sua (Manager vira Admin global)
                if (string.Equals(request.Role, IsmRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_currentUser.Role, IsmRoles.Manager, StringComparison.OrdinalIgnoreCase))
                {
                    // Manager pode promover a Admin DO MESMO restaurante (com restaurantId setado), só não pode SuperAdmin
                }
            }
        }

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

        if (!IsmRoles.IsValid(request.Role))
            throw new InvalidOperationException($"Role inválido '{request.Role}'. Roles permitidos: {string.Join(", ", IsmRoles.AllAllowed)}");

        var normalizedRole = IsmRoles.Normalize(request.Role);

        user.Name = request.Name;
        user.Email = request.Email;
        user.Role = normalizedRole;
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

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem ativar/desativar usuários.");

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (user.RestaurantId != _currentUser.RestaurantId.Value)
                throw new UnauthorizedAccessException("Você só pode ativar/desativar usuários do seu próprio restaurante.");
        }

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

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem deletar usuários.");

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (user.RestaurantId != _currentUser.RestaurantId.Value)
                return false;
        }

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
