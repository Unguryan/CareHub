namespace CareHub.Document.Models;

public class StoredDocument
{
    public Guid Id { get; set; }
    public DocumentKind Kind { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public string StorageKey { get; set; } = "";
    public string? Sha256 { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DocumentSource Source { get; set; }
}
