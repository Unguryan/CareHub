namespace CareHub.Audit.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public DateTime RecordedAt { get; set; }
    public string ActionType { get; set; } = "";
    public Guid? ActorUserId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? BranchId { get; set; }
    public string DetailsJson { get; set; } = "";
    public string? BrokerMessageId { get; set; }
}
