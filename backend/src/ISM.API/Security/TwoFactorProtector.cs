using ISM.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace ISM.API.Security;

public sealed class TwoFactorProtector(IDataProtectionProvider provider) : ITwoFactorProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("ISM.TwoFactor.v1");
    public string Protect(string secret) => _protector.Protect(secret);
    public string Unprotect(string protectedSecret) => _protector.Unprotect(protectedSecret);
}
