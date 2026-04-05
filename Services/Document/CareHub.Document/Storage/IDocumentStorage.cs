namespace CareHub.Document.Storage;

public interface IDocumentStorage
{
    Task WriteAsync(string storageKey, byte[] content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
