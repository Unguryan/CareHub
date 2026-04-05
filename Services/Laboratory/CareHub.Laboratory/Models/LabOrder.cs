namespace CareHub.Laboratory.Models;

public class LabOrder
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid BranchId { get; set; }
    public LabOrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SampleReceivedAt { get; set; }
    public string? ResultSummary { get; set; }
    public DateTime? ResultEnteredAt { get; set; }
    public Guid? ResultEnteredByUserId { get; set; }
}
