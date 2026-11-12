using Dapper;
using Voicepia.Api.Models.Workspace;

namespace Voicepia.Api.Gateways;

// documents 테이블 전용. 모든 SQL에 user_id 조건을 넣는다.
public class DocumentGateway : GatewayBase
{
    public DocumentGateway(IConfiguration config) : base(config) { }

    private const string Columns =
        "document_id, user_id, folder_id, title, content, sort_order, created_at, updated_at";

    // 탐색기용 목록: 본문은 빼고 글자 수만.
    // 화면의 "공백 포함" 글자 수와 맞추려고 줄바꿈은 빼고 센다.
    public async Task<List<DocumentSummaryInfoModel>> ListSummariesByUserAsync(long userId)
    {
        const string sql = @"
            SELECT document_id, folder_id, title, sort_order,
                   char_length(regexp_replace(content, '\r?\n', '', 'g')) AS content_length, updated_at
            FROM documents
            WHERE user_id = @userId
            ORDER BY sort_order, document_id";
        await using var conn = await OpenAsync();
        var rows = await conn.QueryAsync<DocumentSummaryInfoModel>(sql, new { userId });
        return rows.ToList();
    }

    public async Task<DocumentModel?> ReadAsync(long userId, long documentId)
    {
        var sql = $"SELECT {Columns} FROM documents WHERE user_id = @userId AND document_id = @documentId";
        await using var conn = await OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<DocumentModel>(sql, new { userId, documentId });
    }

    public async Task<DocumentModel> InsertAsync(long userId, long? folderId, string title, string content = "")
    {
        var sql = $@"
            INSERT INTO documents (user_id, folder_id, title, content, sort_order)
            VALUES (
                @userId, @folderId, @title, @content,
                COALESCE((SELECT MAX(sort_order) FROM documents
                          WHERE user_id = @userId AND folder_id IS NOT DISTINCT FROM @folderId), 0) + 1
            )
            RETURNING {Columns}";
        await using var conn = await OpenAsync();
        return await conn.QuerySingleAsync<DocumentModel>(sql, new { userId, folderId, title, content });
    }

    // 제목과 본문 중 보낸 것만 바꾼다. COALESCE(@값, 기존값): 값이 NULL이면 기존 값을 그대로 둔다.
    public async Task<DocumentModel?> UpdateAsync(long userId, long documentId, string? title, string? content)
    {
        var sql = $@"
            UPDATE documents SET
                title      = COALESCE(@title, title),
                content    = COALESCE(@content, content),
                updated_at = now()
            WHERE user_id = @userId AND document_id = @documentId
            RETURNING {Columns}";
        await using var conn = await OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<DocumentModel>(sql, new { userId, documentId, title, content });
    }

    public async Task<int> DeleteAsync(long userId, long documentId)
    {
        const string sql = "DELETE FROM documents WHERE user_id = @userId AND document_id = @documentId";
        await using var conn = await OpenAsync();
        return await conn.ExecuteAsync(sql, new { userId, documentId });
    }
}
