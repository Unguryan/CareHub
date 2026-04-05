namespace CareHub.Document.Clients;

public interface ILaboratoryInternalClient
{
    Task<LaboratoryDocumentContextDto?> GetLabOrderDocumentContextAsync(
        Guid labOrderId,
        CancellationToken cancellationToken = default);
}
