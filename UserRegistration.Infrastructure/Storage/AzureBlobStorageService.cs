using System.Collections.Concurrent;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;

    // CreateIfNotExistsAsync is already an idempotent "check, then create if missing"
    // round trip, so this cache is purely an optimization to skip repeating that round
    // trip on every upload once we know a container is there — never a source of truth.
    // Only successes are recorded, so a transient failure (e.g. a network blip) doesn't
    // "poison" the container forever; the next upload just retries the check.
    private readonly ConcurrentDictionary<string, byte> _containersKnownToExist = new(StringComparer.OrdinalIgnoreCase);

    public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
    {
        _serviceClient = new BlobServiceClient(options.Value.ConnectionString);
    }

    public async Task<string> UploadAsync(
        string containerName,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        await EnsureContainerExistsAsync(containerName, containerClient, cancellationToken);

        // blobPath may contain "/" (e.g. "images/{guid}.jpg" or "{userId}/{guid}.jpg") —
        // Azure Blob Storage has no real subfolders, a blob name containing "/" is all a
        // virtual folder is; nothing extra needs to be created for it to "exist".
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.UploadAsync(
            content,
            // Public read access on the blob (not the container listing) — images are
            // rendered straight into <img src="…"> on the reading side, unauthenticated.
            // Requires "Allow Blob public access" enabled on the storage account itself.
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl) || !Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        // A BlobUriBuilder parses "https://{account}.blob.core.windows.net/{container}/{blobPath}"
        // (exactly what UploadAsync returns) back into its container/blob parts, so callers
        // never have to reconstruct that split themselves.
        BlobUriBuilder builder;
        try
        {
            builder = new BlobUriBuilder(uri);
        }
        catch (FormatException)
        {
            // Not a blob-storage URL at all (e.g. a URL from a different host) — nothing
            // of ours to delete.
            return;
        }

        if (string.IsNullOrEmpty(builder.BlobContainerName) || string.IsNullOrEmpty(builder.BlobName))
        {
            return;
        }

        var containerClient = _serviceClient.GetBlobContainerClient(builder.BlobContainerName);
        await containerClient.DeleteBlobIfExistsAsync(builder.BlobName, cancellationToken: cancellationToken);
    }

    private async Task EnsureContainerExistsAsync(
        string containerName,
        BlobContainerClient containerClient,
        CancellationToken cancellationToken)
    {
        if (_containersKnownToExist.ContainsKey(containerName))
        {
            return;
        }

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        _containersKnownToExist.TryAdd(containerName, 0);
    }
}
