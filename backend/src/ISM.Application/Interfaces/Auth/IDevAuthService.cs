using System.Threading;
using System.Threading.Tasks;
using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IDevAuthService
{
    Task<DevAuthResponse> GenerateDevTokenAsync(GenerateDevTokenRequest? request, CancellationToken cancellationToken = default);
}
