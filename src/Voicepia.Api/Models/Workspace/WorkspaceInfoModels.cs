namespace Voicepia.Api.Models.Workspace;

// 왼쪽 탐색기에 보여줄 글 정보 (본문은 무거우니 빼고, 글자 수만 담는다)
public class DocumentSummaryInfoModel
{
    public long DocumentId { get; set; }
    public long? FolderId { get; set; }
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public int ContentLength { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// 탐색기 전체 (폴더 목록 + 글 목록). 트리 모양은 화면이 parent id로 조립한다.
public class WorkspaceTreeInfoModel
{
    public List<FolderModel> Folders { get; set; } = [];
    public List<DocumentSummaryInfoModel> Documents { get; set; } = [];
}
