using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;

namespace RepairDesk.Api.Services;

public class AuthService : IAuthService
{
    private readonly RepairDeskDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public AuthService(RepairDeskDbContext db, IPasswordHasher passwordHasher, IJwtTokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt)
            ? _tokenService.CreateToken(user)
            : null;
    }
}
