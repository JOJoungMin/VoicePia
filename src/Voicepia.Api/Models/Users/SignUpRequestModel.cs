namespace Voicepia.Api.Models.Users;

public class SignUpRequestModel
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string PenName { get; set; } = "";
}
