namespace UserRegistration.Application.Common.Interfaces;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads content to <paramref name="blobPath"/> inside <paramref name="containerName"/>
    /// and returns its public URL. The container is created automatically if it doesn't
    /// already exist. <paramref name="blobPath"/> may include "/" to place the blob under a
    /// virtual folder (e.g. "images/abc123.jpg" or "{userId}/abc123.jpg") — Azure Blob
    /// Storage containers don't have real subfolders, but a "/" in a blob name renders as
    /// one everywhere blobs are listed or browsed.
    /// </summary>
    Task<string> UploadAsync(
        string containerName,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the blob a previously-returned public URL points at. Used to
    /// clean up storage when a rejected post (or an abandoned, never-saved
    /// draft) is removed. A URL that doesn't parse to a blob in this account,
    /// or one that's already gone, is treated as a no-op rather than an error
    /// — there's nothing left to delete either way.
    /// </summary>
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
