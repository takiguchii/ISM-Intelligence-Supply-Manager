using ISM.Application.Modules.System.DTOs;

namespace ISM.Application.Abstractions;

public interface ISystemStatusService
{
    Task<SystemStatusResponse> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}
