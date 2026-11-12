namespace Voicepia.Api.Models.Workspace;

// documents 테이블과 1:1
public class DocumentModel
{
    public long DocumentId { get; set; }
    public long UserId { get; set; }
    public long? FolderId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
