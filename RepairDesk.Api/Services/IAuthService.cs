namespace RepairDesk.Api.Services;

public interface IAuthService
{
    Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
