using Microsoft.AspNetCore.Identity;
using Npgsql;
using Voicepia.Api.Common;
using Voicepia.Api.Gateways;
using Voicepia.Api.Models.Users;

namespace Voicepia.Api.Services;

public class UserService
{
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
            SecurityStamp = Guid.NewGuid().ToString("N"),
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
}
