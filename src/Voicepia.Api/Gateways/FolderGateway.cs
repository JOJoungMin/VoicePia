using Dapper;
using Voicepia.Api.Models.Workspace;

namespace Voicepia.Api.Gateways;

// folders 테이블 전용. 모든 SQL에 user_id 조건을 넣어서, 남의 폴더는 아예 보이지도 바뀌지도 않게 한다.
// UserGateway와 달리 Dapper를 쓴다: SELECT 결과(folder_id, parent_folder_id ...)를
// 같은 이름의 C# 속성(FolderId, ParentFolderId ...)에 자동으로 채워 준다.
public class FolderGateway : GatewayBase
{
    public FolderGateway(IConfiguration config) : base(config) { }

    private const string Columns =
        "folder_id, user_id, parent_folder_id, name, sort_order, created_at, updated_at";

    public async Task<List<FolderModel>> ListByUserAsync(long userId)
    {
        var sql = $"SELECT {Columns} FROM folders WHERE user_id = @userId ORDER BY sort_order, folder_id";
        await using var conn = await OpenAsync();
        var rows = await conn.QueryAsync<FolderModel>(sql, new { userId });
        return rows.ToList();
    }

    public async Task<FolderModel?> ReadAsync(long userId, long folderId)
    {
        var sql = $"SELECT {Columns} FROM folders WHERE user_id = @userId AND folder_id = @folderId";
        await using var conn = await OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<FolderModel>(sql, new { userId, folderId });
    }

    // 같은 폴더 안의 마지막 순서 + 1 로 넣는다.
    // IS NOT DISTINCT FROM: "= 와 같지만 NULL끼리도 같다고 본다" (맨 위 폴더는 parent가 NULL이라서 필요)
    public async Task<FolderModel> InsertAsync(long userId, long? parentFolderId, string name)
    {
        var sql = $@"
            INSERT INTO folders (user_id, parent_folder_id, name, sort_order)
            VALUES (
                @userId, @parentFolderId, @name,
                COALESCE((SELECT MAX(sort_order) FROM folders
                          WHERE user_id = @userId AND parent_folder_id IS NOT DISTINCT FROM @parentFolderId), 0) + 1
            )
            RETURNING {Columns}";
        await using var conn = await OpenAsync();
        return await conn.QuerySingleAsync<FolderModel>(sql, new { userId, parentFolderId, name });
    }

    // 바뀐 행 수를 돌려준다 (0이면 없는 폴더이거나 남의 폴더)
    public async Task<int> UpdateNameAsync(long userId, long folderId, string name)
    {
        const string sql = @"
            UPDATE folders SET name = @name, updated_at = now()
            WHERE user_id = @userId AND folder_id = @folderId";
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, new { userId, folderId, name });
    }

    // 하위 폴더와 그 안의 글은 DB의 ON DELETE CASCADE가 함께 지운다
    public async Task<int> DeleteAsync(long userId, long folderId)
    {
        const string sql = "DELETE FROM folders WHERE user_id = @userId AND folder_id = @folderId";
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, new { userId, folderId });
    }
}
