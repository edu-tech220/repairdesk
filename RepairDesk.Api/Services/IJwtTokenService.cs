using RepairDesk.Api.Models;

namespace RepairDesk.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(AppUser user);
}
