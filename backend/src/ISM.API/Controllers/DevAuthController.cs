using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/dev")]
[ApiExplorerSettings(GroupName = "v1-dev")]
public sealed class DevAuthController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IDevAuthService _devAuthService;

    public DevAuthController(
        IHostEnvironment environment,
        IDevAuthService devAuthService)
    {
        _environment = environment;
        _devAuthService = devAuthService;
    }

    [HttpPost("token")]
    [ProducesResponseType(typeof(DevAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DevAuthResponse>> GenerateToken(
        [FromBody] GenerateDevTokenRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Este endpoint está disponível APENAS em ambiente de Desenvolvimento. Desativado em Produção.",
                env = _environment.EnvironmentName
            });
        }

        try
        {
            var response = await _devAuthService.GenerateDevTokenAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("token")]
    [ProducesResponseType(typeof(DevAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DevAuthResponse>> GenerateTokenQuick(CancellationToken cancellationToken)
    {
        return await GenerateToken(null, cancellationToken);
    }
}
