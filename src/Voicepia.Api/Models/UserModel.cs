namespace Voicepia.Api.Models;

public class UserModel
{
    public long UserId { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PenName { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string SecurityStamp { get; set; } = "";
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}