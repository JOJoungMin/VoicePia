namespace Voicepia.Api.Models.Workspace;

// folders 테이블과 1:1
public class FolderModel
{
    public long FolderId { get; set; }
    public long UserId { get; set; }
    public long? ParentFolderId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
