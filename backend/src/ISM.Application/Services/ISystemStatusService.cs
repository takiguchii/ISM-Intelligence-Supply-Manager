using ISM.Application.DTOs;

namespace ISM.Application.Services;

public interface ISystemStatusService
{
    Task<SystemStatusResponse> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}
