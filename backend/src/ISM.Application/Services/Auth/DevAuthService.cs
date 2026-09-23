using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Auth;

public sealed class DevAuthService : IDevAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;

    public DevAuthService(IUserRepository userRepository, IAuthService authService)
    {
        _userRepository = userRepository;
        _authService = authService;
    }

    public async Task<DevAuthResponse> GenerateDevTokenAsync(
        GenerateDevTokenRequest? request,
        CancellationToken cancellationToken = default)
    {
        User? user = null;
        string source = "admin-default";

        if (request != null)
        {
            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
                source = $"userId={request.UserId.Value}";
                if (user == null)
                    throw new KeyNotFoundException($"Usuário com ID {request.UserId.Value} não encontrado.");
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                source = $"email={request.Email}";
                if (user == null)
                    throw new KeyNotFoundException($"Usuário com e-mail '{request.Email}' não encontrado.");
            }
            else if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var all = await _userRepository.GetAllAsync(cancellationToken);
                user = request.RestaurantId.HasValue
                    ? all.FirstOrDefault(u =>
                        u.Role.Equals(request.Role, StringComparison.OrdinalIgnoreCase) &&
                        u.RestaurantId == request.RestaurantId.Value)
                    : all.FirstOrDefault(u =>
                        u.Role.Equals(request.Role, StringComparison.OrdinalIgnoreCase));

                source = $"role={request.Role}" + (request.RestaurantId.HasValue ? $",restaurantId={request.RestaurantId}" : "");

                if (user == null)
                    throw new KeyNotFoundException($"Nenhum usuário encontrado com role '{request.Role}'" +
                                                  (request.RestaurantId.HasValue ? $" no restaurante {request.RestaurantId}" : ""));
            }
        }

        if (user == null)
        {
            var adminResponse = await _authService.GenerateAdminTokenAsync(cancellationToken);
            return new DevAuthResponse
            {
                Source = source + " (admin padrão ID=1)",
                Token = adminResponse.Token,
                ExpiresAt = adminResponse.ExpiresAt,
                User = adminResponse.User
            };
        }

        var tokenResponse = await _authService.GenerateTokenForUserAsync(user, cancellationToken);
        return new DevAuthResponse
        {
            Source = source,
            Token = tokenResponse.Token,
            ExpiresAt = tokenResponse.ExpiresAt,
            User = tokenResponse.User
        };
    }
}
