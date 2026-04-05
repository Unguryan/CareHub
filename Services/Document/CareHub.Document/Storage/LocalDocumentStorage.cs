namespace CareHub.Document.Storage;

public sealed class LocalDocumentStorage : IDocumentStorage
{
    private readonly string _root;

    public LocalDocumentStorage(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
    }

    public Task WriteAsync(string storageKey, byte[] content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var full = GetFullPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return File.WriteAllBytesAsync(full, content, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var full = GetFullPath(storageKey);
        if (!File.Exists(full))
            throw new FileNotFoundException("Document file missing on storage.", full);
        return Task.FromResult<Stream>(File.OpenRead(full));
    }

    private string GetFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        var combined = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!combined.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage key path.");
        return combined;
    }
}
