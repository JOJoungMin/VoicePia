using System.Security.Claims;

namespace Voicepia.Api.Common;

public static class ClaimsPrincipalExtensions
{
    // 로그인 쿠키에 들어 있는 회원 번호. [Authorize]가 붙은 곳에서만 부른다.
    public static long GetUserId(this ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
