namespace Voicepia.Api.Models.Users;

// 화면에 돌려주는 회원 정보 (비밀번호 해시 등 민감한 값은 넣지 않는다)
public class UserInfoModel
{
    public long UserId { get; set; }
    public string Email { get; set; } = "";
    public string PenName { get; set; } = "";
}
