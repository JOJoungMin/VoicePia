namespace Voicepia.Api.Models.Workspace;

public class FolderCreateRequestModel
{
    public long? ParentFolderId { get; set; }   // null이면 맨 위
    public string Name { get; set; } = "";
}

public class FolderRenameRequestModel
{
    public string Name { get; set; } = "";
}

public class DocumentCreateRequestModel
{
    public long? FolderId { get; set; }         // null이면 맨 위
    public string Title { get; set; } = "";
}

// 바꿀 것만 보낸다 (제목만, 본문만, 또는 둘 다)
public class DocumentUpdateRequestModel
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}
