using System.ComponentModel.DataAnnotations;

namespace RepairDesk.Api.Dtos;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record LoginResponse(string Token, string Username, string Role);
