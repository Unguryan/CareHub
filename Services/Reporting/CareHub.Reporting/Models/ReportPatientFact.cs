namespace CareHub.Reporting.Models;

public class ReportPatientFact
{
    public Guid PatientId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
}
