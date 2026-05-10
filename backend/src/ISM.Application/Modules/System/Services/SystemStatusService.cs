using ISM.Application.Abstractions;
using ISM.Application.Modules.System.DTOs;
using ISM.Domain.Modules.System.Interfaces;

namespace ISM.Application.Modules.System.Services;

public sealed class SystemStatusService : ISystemStatusService
{
    private readonly IPlatformModuleRepository _platformModuleRepository;

    public SystemStatusService(IPlatformModuleRepository platformModuleRepository)
    {
        _platformModuleRepository = platformModuleRepository;
    }

    public async Task<SystemStatusResponse> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        var databaseConnected = await _platformModuleRepository.CanConnectAsync(cancellationToken);

        if (!databaseConnected)
        {
            return new SystemStatusResponse(
                "ISM - Intelligence Supply Manager",
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                DateTime.UtcNow,
                false,
                0,
                null,
                Array.Empty<EnabledModuleResponse>());
        }

        var enabledModules = await _platformModuleRepository.GetEnabledOrderedAsync(cancellationToken);
        var firstEnabledModule = await _platformModuleRepository.GetFirstEnabledAsync(cancellationToken);
        var enabledModuleCount = await _platformModuleRepository.CountEnabledAsync(cancellationToken);

        return new SystemStatusResponse(
            "ISM - Intelligence Supply Manager",
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            DateTime.UtcNow,
            true,
            enabledModuleCount,
            firstEnabledModule is null
                ? null
                : new EnabledModuleResponse(
                    firstEnabledModule.Id,
                    firstEnabledModule.Name,
                    firstEnabledModule.Slug,
                    firstEnabledModule.Description,
                    firstEnabledModule.SortOrder),
            enabledModules
                .Select(module => new EnabledModuleResponse(
                    module.Id,
                    module.Name,
                    module.Slug,
                    module.Description,
                    module.SortOrder))
                .ToArray());
    }
}
