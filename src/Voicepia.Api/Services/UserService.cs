using Microsoft.AspNetCore.Identity;
using Npgsql;
using Voicepia.Api.Common;
using Voicepia.Api.Gateways;
using Voicepia.Api.Models.Users;

namespace Voicepia.Api.Services;

public class UserService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly UserGateway _users;
    private readonly IPasswordHasher<UserModel> _hasher;

    public UserService(UserGateway users, IPasswordHasher<UserModel> hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<Result<long>> SignUpAsync(SignUpRequestModel input)
    {
        // 1. 입력 정리
        var email = input.Email.Trim().ToLowerInvariant();
        var penName = input.PenName.Trim();

        // 2. 입력 검사
        if (!email.Contains('@'))
            return Result<long>.Fail("InvalidEmail", "이메일 형식이 올바르지 않습니다.");
        if (input.Password.Length < 8)
            return Result<long>.Fail("WeakPassword", "비밀번호는 8자 이상이어야 합니다.");
        if (penName.Length == 0)
            return Result<long>.Fail("PenNameRequired", "필명을 입력하세요.");

        // 3. 저장할 회원 만들기
        var user = new UserModel
        {
            Email = email,
            PenName = penName,
            SecurityStamp = NewSecurityStamp(),
        };
        user.PasswordHash = _hasher.HashPassword(user, input.Password);

        // 4. 저장 (중복은 DB 제약이 잡아줌)
        try
        {
            var userId = await _users.InsertAsync(user);
            return Result<long>.Ok(userId);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return ex.ConstraintName == "uq_users_pen_name"
                ? Result<long>.Fail("DuplicatePenName", "이미 사용 중인 필명입니다.")
                : Result<long>.Fail("DuplicateEmail", "이미 가입된 이메일입니다.");
        }
    }

    public async Task<Result<UserModel>> LoginAsync(LoginRequestModel input)
    {
        const string invalidMessage = "이메일 또는 비밀번호가 올바르지 않습니다.";

        // 1. 입력 정리
        var email = input.Email.Trim().ToLowerInvariant();

        // 2. 회원 찾기
        var user = await _users.ReadByEmailAsync(email);
        if (user == null)
            return Result<UserModel>.Fail("InvalidCredentials", invalidMessage);

        // 3. 잠금 확인
        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            return Result<UserModel>.Fail("LockedOut", "로그인 시도가 너무 많아 잠시 잠겼습니다. 잠시 후 다시 시도하세요.");

        // 4. 비밀번호 확인
        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            await _users.RecordLoginFailureAsync(user.UserId, MaxFailedAttempts, LockoutMinutes);
            return Result<UserModel>.Fail("InvalidCredentials", invalidMessage);
        }

        // 5. 성공 기록
        await _users.RecordLoginSuccessAsync(user.UserId);
        return Result<UserModel>.Ok(user);
    }

    // 로그인 쿠키에 들어 있는 stamp가 DB의 현재 값과 같은지 확인
    public async Task<bool> IsSecurityStampValidAsync(long userId, string securityStamp)
    {
        var current = await _users.ReadSecurityStampAsync(userId);
        return current != null && current == securityStamp;
    }

    // stamp를 새로 갈면, 예전 stamp를 가진 모든 기기의 로그인 쿠키가 무효가 된다
    public async Task<Result<bool>> LogoutAllDevicesAsync(long userId)
    {
        await _users.UpdateSecurityStampAsync(userId, NewSecurityStamp());
        return Result<bool>.Ok(true);
    }

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}
