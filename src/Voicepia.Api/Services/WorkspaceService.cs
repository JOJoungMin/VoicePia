using Voicepia.Api.Common;
using Voicepia.Api.Gateways;
using Voicepia.Api.Models.Workspace;

namespace Voicepia.Api.Services;

// 작성 화면(폴더 + 글)의 업무 규칙
//  - 모든 메서드는 "로그인한 사람(userId)"의 데이터만 다룬다
//  - 부모 폴더는 반드시 같은 사람의 폴더여야 한다 (남의 폴더에 넣기 금지)
//  - 이름/제목은 앞뒤 공백을 지우고, 비어 있으면 안 되고, 100자를 넘으면 안 된다
public class WorkspaceService
{
    private const int MaxNameLength = 100;

    private readonly FolderGateway _folders;
    private readonly DocumentGateway _documents;

    public WorkspaceService(FolderGateway folders, DocumentGateway documents)
    {
        _folders = folders;
        _documents = documents;
    }

    // ---- 탐색기 전체 ----

    public async Task<Result<WorkspaceTreeInfoModel>> GetTreeAsync(long userId)
    {
        var tree = new WorkspaceTreeInfoModel
        {
            Folders = await _folders.ListByUserAsync(userId),
            Documents = await _documents.ListSummariesByUserAsync(userId),
        };
        return Result<WorkspaceTreeInfoModel>.Ok(tree);
    }

    // ---- 폴더 ----

    public async Task<Result<FolderModel>> CreateFolderAsync(long userId, FolderCreateRequestModel input)
    {
        var name = CleanName(input.Name);
        if (name == null) return Result<FolderModel>.Fail("NameRequired", NameMessage);

        if (input.ParentFolderId != null && await _folders.ReadAsync(userId, input.ParentFolderId.Value) == null)
            return Result<FolderModel>.Fail("ParentNotFound", "상위 폴더를 찾을 수 없습니다.");

        var folder = await _folders.InsertAsync(userId, input.ParentFolderId, name);
        return Result<FolderModel>.Ok(folder);
    }

    public async Task<Result<bool>> RenameFolderAsync(long userId, long folderId, FolderRenameRequestModel input)
    {
        var name = CleanName(input.Name);
        if (name == null) return Result<bool>.Fail("NameRequired", NameMessage);

        var changed = await _folders.UpdateNameAsync(userId, folderId, name);
        return changed > 0 ? Result<bool>.Ok(true) : NotFound<bool>("폴더");
    }

    public async Task<Result<bool>> DeleteFolderAsync(long userId, long folderId)
    {
        var deleted = await _folders.DeleteAsync(userId, folderId);
        return deleted > 0 ? Result<bool>.Ok(true) : NotFound<bool>("폴더");
    }

    // ---- 글 ----

    public async Task<Result<DocumentModel>> CreateDocumentAsync(long userId, DocumentCreateRequestModel input)
    {
        var title = CleanName(input.Title);
        if (title == null) return Result<DocumentModel>.Fail("NameRequired", NameMessage);

        if (input.FolderId != null && await _folders.ReadAsync(userId, input.FolderId.Value) == null)
            return Result<DocumentModel>.Fail("ParentNotFound", "글을 넣을 폴더를 찾을 수 없습니다.");

        var doc = await _documents.InsertAsync(userId, input.FolderId, title);
        return Result<DocumentModel>.Ok(doc);
    }

    public async Task<Result<DocumentModel>> GetDocumentAsync(long userId, long documentId)
    {
        var doc = await _documents.ReadAsync(userId, documentId);
        return doc != null ? Result<DocumentModel>.Ok(doc) : NotFound<DocumentModel>("글");
    }

    public async Task<Result<DocumentModel>> UpdateDocumentAsync(long userId, long documentId, DocumentUpdateRequestModel input)
    {
        string? title = null;
        if (input.Title != null)
        {
            title = CleanName(input.Title);
            if (title == null) return Result<DocumentModel>.Fail("NameRequired", NameMessage);
        }

        var doc = await _documents.UpdateAsync(userId, documentId, title, input.Content);
        return doc != null ? Result<DocumentModel>.Ok(doc) : NotFound<DocumentModel>("글");
    }

    public async Task<Result<bool>> DeleteDocumentAsync(long userId, long documentId)
    {
        var deleted = await _documents.DeleteAsync(userId, documentId);
        return deleted > 0 ? Result<bool>.Ok(true) : NotFound<bool>("글");
    }

    // ---- 공통 ----

    private const string NameMessage = "이름은 1자 이상 100자 이하로 입력하세요.";

    private static string? CleanName(string? raw)
    {
        var name = raw?.Trim() ?? "";
        return name.Length is > 0 and <= MaxNameLength ? name : null;
    }

    // 없는 것과 남의 것을 구분하지 않고 똑같이 "찾을 수 없다"고 답한다
    // (남의 글 번호가 존재하는지조차 알려주지 않기 위해)
    private static Result<T> NotFound<T>(string what) =>
        Result<T>.Fail("NotFound", $"{what}을(를) 찾을 수 없습니다.");
}
