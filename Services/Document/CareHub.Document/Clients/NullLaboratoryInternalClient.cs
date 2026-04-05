namespace CareHub.Document.Clients;

public sealed class NullLaboratoryInternalClient : ILaboratoryInternalClient
{
    public Task<LaboratoryDocumentContextDto?> GetLabOrderDocumentContextAsync(
        Guid labOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<LaboratoryDocumentContextDto?>(null);
}
