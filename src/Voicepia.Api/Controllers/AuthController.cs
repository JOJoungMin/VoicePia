using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voicepia.Api.Common;
using Voicepia.Api.Models.Users;
using Voicepia.Api.Services;

namespace Voicepia.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;

    public AuthController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<Result<long>>> SignUp(SignUpRequestModel input)
    {
        var result = await _userService.SignUpAsync(input);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result<UserInfoModel>>> Login(LoginRequestModel input)
    {
        var result = await _userService.LoginAsync(input);
        if (!result.Success)
            return Unauthorized(Result<UserInfoModel>.Fail(result.Code, result.Message));

        var user = result.Data!;

        // 쿠키 안에 넣어 둘 회원 정보 (Claim = "이 사람에 대한 사실 한 줄")
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.PenName),
            new("security_stamp", user.SecurityStamp),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // 로그인 쿠키 발급 (응답의 Set-Cookie 헤더로 브라우저에 전달됨)
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(Result<UserInfoModel>.Ok(new UserInfoModel
        {
            UserId = user.UserId,
            Email = user.Email,
            PenName = user.PenName,
        }));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<Result<UserInfoModel>> Me()
    {
        // 로그인 쿠키에서 꺼낸 정보 (DB를 다시 조회하지 않음)
        return Ok(Result<UserInfoModel>.Ok(new UserInfoModel
        {
            UserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "",
            PenName = User.FindFirstValue(ClaimTypes.Name) ?? "",
        }));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<Result<bool>>> Logout()
    {
        // 로그인 쿠키 삭제
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(Result<bool>.Ok(true));
    }
}
