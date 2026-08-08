using ISM.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/system")]
[AllowAnonymous]
public sealed class SystemController : ControllerBase
{
    private readonly IsmDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public SystemController(IsmDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Status(CancellationToken cancellationToken)
    {
        bool dbConnected;
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            dbConnected = true;
        }
        catch
        {
            dbConnected = false;
        }

        var enabledModules = new[]
        {
            new
            {
                id = 1,
                name = "Fornecedores",
                slug = "suppliers",
                description = "Gestão de fornecedores e parceiros comerciais",
                sortOrder = 1
            },
            new
            {
                id = 2,
                name = "Estoque",
                slug = "stock",
                description = "Controle de produtos e movimentações de estoque",
                sortOrder = 2
            },
            new
            {
                id = 3,
                name = "Restaurantes",
                slug = "restaurants",
                description = "Cadastro e gestão de restaurantes",
                sortOrder = 3
            },
            new
            {
                id = 4,
                name = "Cardápio",
                slug = "menu",
                description = "Categorias, pratos e composições do menu",
                sortOrder = 4
            }
        };

        return Ok(new
        {
            applicationName = "ISM - Intelligence Supply Manager",
            environment = _env.EnvironmentName,
            utcTimestamp = DateTime.UtcNow.ToString("O"),
            databaseConnected = dbConnected,
            enabledModuleCount = enabledModules.Length,
            firstEnabledModule = enabledModules[0],
            enabledModules = enabledModules
        });
    }
}
