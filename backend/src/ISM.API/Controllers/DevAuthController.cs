using System.ComponentModel.DataAnnotations;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/dev")]
[ApiExplorerSettings(GroupName = "v1-dev")]
public sealed class DevAuthController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;

    public DevAuthController(
        IHostEnvironment environment,
        IAuthService authService,
        IUserRepository userRepository)
    {
        _environment = environment;
        _authService = authService;
        _userRepository = userRepository;
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> GenerateToken(
        [FromBody] GenerateDevTokenRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Este endpoint está disponível APENAS em ambiente de Desenvolvimento. Desativado em Produção.",
                env = _environment.EnvironmentName
            });

        User? user = null;
        string source = "admin-default";

        if (request != null)
        {
            if (request.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
                source = $"userId={request.UserId.Value}";
                if (user == null)
                    return BadRequest(new { message = $"Usuário com ID {request.UserId.Value} não encontrado." });
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                source = $"email={request.Email}";
                if (user == null)
                    return BadRequest(new { message = $"Usuário com e-mail '{request.Email}' não encontrado." });
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
                    return BadRequest(new
                    {
                        message = $"Nenhum usuário encontrado com role '{request.Role}'" +
                                  (request.RestaurantId.HasValue ? $" no restaurante {request.RestaurantId}" : "")
                    });
            }
        }

        if (user == null)
        {
            var response = await _authService.GenerateAdminTokenAsync(cancellationToken);
            return Ok(new
            {
                source = source + " (admin padrão ID=1)",
                token = response.Token,
                expiresAt = response.ExpiresAt,
                user = response.User
            });
        }

        var tokenResponse = await _authService.GenerateTokenForUserAsync(user, cancellationToken);
        return Ok(new
        {
            source,
            token = tokenResponse.Token,
            expiresAt = tokenResponse.ExpiresAt,
            user = tokenResponse.User
        });
    }

    [HttpGet("token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GenerateTokenQuick(CancellationToken cancellationToken)
    {
        var result = await GenerateToken(null, cancellationToken);
        return result.Result;
    }
}

public sealed class GenerateDevTokenRequest
{
    public int? UserId { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string? Email { get; set; }

    public string? Role { get; set; }

    public int? RestaurantId { get; set; }
}
