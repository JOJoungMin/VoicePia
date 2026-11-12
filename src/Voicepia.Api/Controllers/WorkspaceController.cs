using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voicepia.Api.Common;
using Voicepia.Api.Models.Workspace;
using Voicepia.Api.Services;

namespace Voicepia.Api.Controllers;

// 작성 화면 API. 전부 로그인이 필요하고, 항상 "로그인한 사람"의 데이터만 다룬다.
//
//   GET    /api/workspace/tree                탐색기 전체 (폴더 + 글 목록)
//   POST   /api/workspace/folders             폴더 만들기
//   PATCH  /api/workspace/folders/{id}        폴더 이름 바꾸기
//   DELETE /api/workspace/folders/{id}        폴더 삭제 (안의 폴더·글까지)
//   POST   /api/workspace/documents           글 만들기
//   GET    /api/workspace/documents/{id}      글 하나 (본문 포함)
//   PATCH  /api/workspace/documents/{id}      글 제목/본문 바꾸기
//   DELETE /api/workspace/documents/{id}      글 삭제
[ApiController]
[Authorize]
[Route("api/workspace")]
public class WorkspaceController : ControllerBase
{
    private readonly WorkspaceService _service;

    public WorkspaceController(WorkspaceService service)
    {
        _service = service;
    }

    [HttpGet("tree")]
    public async Task<ActionResult<Result<WorkspaceTreeInfoModel>>> GetTree() =>
        ToResponse(await _service.GetTreeAsync(User.GetUserId()));

    [HttpPost("folders")]
    public async Task<ActionResult<Result<FolderModel>>> CreateFolder(FolderCreateRequestModel input) =>
        ToResponse(await _service.CreateFolderAsync(User.GetUserId(), input));

    [HttpPatch("folders/{folderId:long}")]
    public async Task<ActionResult<Result<bool>>> RenameFolder(long folderId, FolderRenameRequestModel input) =>
        ToResponse(await _service.RenameFolderAsync(User.GetUserId(), folderId, input));

    [HttpDelete("folders/{folderId:long}")]
    public async Task<ActionResult<Result<bool>>> DeleteFolder(long folderId) =>
        ToResponse(await _service.DeleteFolderAsync(User.GetUserId(), folderId));

    [HttpPost("documents")]
    public async Task<ActionResult<Result<DocumentModel>>> CreateDocument(DocumentCreateRequestModel input) =>
        ToResponse(await _service.CreateDocumentAsync(User.GetUserId(), input));

    [HttpGet("documents/{documentId:long}")]
    public async Task<ActionResult<Result<DocumentModel>>> GetDocument(long documentId) =>
        ToResponse(await _service.GetDocumentAsync(User.GetUserId(), documentId));

    [HttpPatch("documents/{documentId:long}")]
    public async Task<ActionResult<Result<DocumentModel>>> UpdateDocument(long documentId, DocumentUpdateRequestModel input) =>
        ToResponse(await _service.UpdateDocumentAsync(User.GetUserId(), documentId, input));

    [HttpDelete("documents/{documentId:long}")]
    public async Task<ActionResult<Result<bool>>> DeleteDocument(long documentId) =>
        ToResponse(await _service.DeleteDocumentAsync(User.GetUserId(), documentId));

    // Result → HTTP 상태 코드: 성공 200, 없음 404, 나머지 실패 400
    private ActionResult<Result<T>> ToResponse<T>(Result<T> result)
    {
        if (result.Success) return Ok(result);
        if (result.Code == "NotFound") return NotFound(result);
        return BadRequest(result);
    }
}
