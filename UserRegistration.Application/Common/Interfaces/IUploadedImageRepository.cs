namespace UserRegistration.Application.Common.Interfaces;

public interface IUploadedImageRepository
{
    Task AddAsync(Domain.Entities.UploadedImage image, CancellationToken cancellationToken = default);

    /// <summary>Links every still-orphaned image this user uploaded that's actually
    /// referenced by the saved post (its cover, or somewhere in the body html) to
    /// that post — determined from the content itself rather than a client-supplied
    /// list, so a discarded upload never gets linked just because it was fetched.</summary>
    Task LinkOrphanedImagesAsync(
        Guid userId,
        int blogPostId,
        string postHtml,
        string? coverImagePath,
        CancellationToken cancellationToken = default);

    /// <summary>Every image (cover + inline body) linked to this post — used
    /// to clean up blob storage when a rejected post is deleted.</summary>
    Task<IReadOnlyList<Domain.Entities.UploadedImage>> GetByBlogPostIdAsync(
        int blogPostId, CancellationToken cancellationToken = default);

    /// <summary>This user's uploads that never made it into a saved post —
    /// used to discard blob storage files for an abandoned compose session
    /// (Clear button / navigating away with unsaved changes).</summary>
    Task<IReadOnlyList<Domain.Entities.UploadedImage>> GetOrphanedByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<Domain.Entities.UploadedImage> images);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
