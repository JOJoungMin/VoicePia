using Npgsql;

namespace Voicepia.Api.Gateways;

public abstract class GatewayBase
{
    private readonly string _connectionString;

    //클래스와 이름이 같고 반환 타입이 없으면 생성자이다.
    protected GatewayBase(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default 가 없습니다.");
    }

    protected async Task<NpgsqlConnection> OpenAsync()
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}