using Npgsql;
using Voicepia.Api.Models.Users;

namespace Voicepia.Api.Gateways;

public class UserGateway : GatewayBase
{
    public UserGateway(IConfiguration config) : base(config) { }

    public async Task<long> InsertAsync(UserModel input)
    {
        const string sql = @"
            INSERT INTO users (email, password_hash, pen_name, security_stamp)
            VALUES (@email, @password_hash, @pen_name, @security_stamp)
            RETURNING user_id";

        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("email", input.Email);
        cmd.Parameters.AddWithValue("password_hash", input.PasswordHash);
        cmd.Parameters.AddWithValue("pen_name", input.PenName);
        cmd.Parameters.AddWithValue("security_stamp", input.SecurityStamp);

        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }

    public async Task<UserModel?> ReadByEmailAsync(string email)
    {
        const string sql = @"
            SELECT user_id, email, password_hash, pen_name, email_confirmed,
                   failed_login_count, lockout_end, security_stamp,
                   last_login_at, created_at, updated_at
            FROM users
            WHERE email = @email";

        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("email", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new UserModel
        {
            UserId           = reader.GetInt64(0),
            Email            = reader.GetString(1),
            PasswordHash     = reader.GetString(2),
            PenName          = reader.GetString(3),
            EmailConfirmed   = reader.GetBoolean(4),
            FailedLoginCount = reader.GetInt32(5),
            LockoutEnd       = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            SecurityStamp    = reader.GetString(7),
            LastLoginAt      = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            CreatedAt        = reader.GetDateTime(9),
            UpdatedAt        = reader.GetDateTime(10),
        };
    }

    public async Task RecordLoginFailureAsync(long userId, int maxAttempts, int lockoutMinutes)
    {
        const string sql = @"
            UPDATE users SET
                failed_login_count = CASE WHEN failed_login_count + 1 >= @max_attempts
                                          THEN 0
                                          ELSE failed_login_count + 1 END,
                lockout_end        = CASE WHEN failed_login_count + 1 >= @max_attempts
                                          THEN now() + make_interval(mins => @lockout_minutes)
                                          ELSE lockout_end END,
                updated_at         = now()
            WHERE user_id = @user_id";

        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("max_attempts", maxAttempts);
        cmd.Parameters.AddWithValue("lockout_minutes", lockoutMinutes);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RecordLoginSuccessAsync(long userId)
    {
        const string sql = @"
            UPDATE users SET
                failed_login_count = 0,
                lockout_end        = NULL,
                last_login_at      = now(),
                updated_at         = now()
            WHERE user_id = @user_id";

        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId);

        await cmd.ExecuteNonQueryAsync();
    }
}