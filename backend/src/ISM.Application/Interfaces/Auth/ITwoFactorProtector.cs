namespace ISM.Application.Interfaces;

public interface ITwoFactorProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}
