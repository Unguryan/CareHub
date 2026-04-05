namespace CareHub.Billing.Models;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid BranchId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public InvoiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaidByUserId { get; set; }
    public DateTime? RefundedAt { get; set; }
    public Guid? RefundedByUserId { get; set; }
    public string? RefundReason { get; set; }
}
