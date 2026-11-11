using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Voicepia.Api.Services;

namespace Voicepia.Api.Common;

// 로그인 쿠키 처리 과정에 끼어들어 우리 규칙을 넣는 클래스
public class AuthCookieEvents : CookieAuthenticationEvents
{
    private readonly UserService _userService;

    public AuthCookieEvents(UserService userService)
    {
        _userService = userService;
    }

    // 로그인 쿠키가 붙은 요청이 올 때마다 실행된다
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue(AuthClaimTypes.SecurityStamp);

        var isValid = long.TryParse(userIdText, out var userId)
                      && stamp != null
                      && await _userService.IsSecurityStampValidAsync(userId, stamp);

        if (!isValid)
        {
            // 이 요청은 "로그인 안 된 사람"으로 처리하고, 브라우저의 쿠키도 지운다
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    // API라서 로그인 페이지로 보내지 않고 상태 코드만 돌려준다
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
