namespace LedgerSystem.Application.Interfaces.Services;

public interface IPasswordService
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hash);
}
