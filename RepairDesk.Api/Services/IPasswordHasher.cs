namespace RepairDesk.Api.Services;

public interface IPasswordHasher
{
    (byte[] Hash, byte[] Salt) HashPassword(string password);
    bool VerifyPassword(string password, byte[] hash, byte[] salt);
}
