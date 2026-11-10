using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Services;

namespace RepairDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly RepairDeskDbContext _db;

    public AuthController(IAuthService authService, RepairDeskDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        if (token is null)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials", Status = StatusCodes.Status401Unauthorized });
        }

        var user = await _db.Users.FirstAsync(u => u.Username == request.Username, cancellationToken);
        return Ok(new LoginResponse(token, user.Username, user.Role.ToString()));
    }
}
