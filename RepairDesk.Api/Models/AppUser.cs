using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
