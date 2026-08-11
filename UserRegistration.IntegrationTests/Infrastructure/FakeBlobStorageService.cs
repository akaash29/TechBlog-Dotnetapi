using System.Collections.Concurrent;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.IntegrationTests.Infrastructure;

/// <summary>Stands in for Azure Blob Storage — tests shouldn't need Azurite
/// or real Azure credentials just to exercise the image upload endpoints.
/// Registered as a singleton (see CustomWebApplicationFactory), so
/// DeletedUrls accumulates across a whole test run — tests that care should
/// assert on the tail of it, not clear it.</summary>
public sealed class FakeBlobStorageService : IBlobStorageService
{
    public ConcurrentBag<string> DeletedUrls { get; } = new();

    public Task<string> UploadAsync(
        string containerName,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://fake-blob-storage.test/{containerName}/{blobPath}");

    public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        DeletedUrls.Add(blobUrl);
        return Task.CompletedTask;
    }
}
