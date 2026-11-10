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
}