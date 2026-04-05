namespace CareHub.Reporting.Models;

public class ReportPaymentFact
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = "";
    public Guid InvoiceId { get; set; }
    public Guid PatientId { get; set; }
    public Guid BranchId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public bool IsRefund { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid ActorUserId { get; set; }
}
