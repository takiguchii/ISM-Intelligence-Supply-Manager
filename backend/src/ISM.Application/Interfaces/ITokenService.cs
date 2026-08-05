using ISM.Domain.Entities;

namespace ISM.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
