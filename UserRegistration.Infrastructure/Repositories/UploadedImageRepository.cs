using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class UploadedImageRepository : IUploadedImageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UploadedImageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UploadedImage image, CancellationToken cancellationToken = default) =>
        await _dbContext.UploadedImages.AddAsync(image, cancellationToken);

    public async Task LinkOrphanedImagesAsync(
        Guid userId,
        int blogPostId,
        string postHtml,
        string? coverImagePath,
        CancellationToken cancellationToken = default)
    {
        // "Is this column value a substring of this literal" isn't something EF Core
        // can translate to SQL, so pull this user's still-unlinked uploads (a small,
        // bounded set) and do the actual matching in memory.
        var candidates = await _dbContext.UploadedImages
            .Where(i => i.CreatedBy == userId && i.BlogPostId == null)
            .ToListAsync(cancellationToken);

        foreach (var image in candidates)
        {
            var isUsed = image.ImagePath == coverImagePath ||
                (!string.IsNullOrEmpty(postHtml) && postHtml.Contains(image.ImagePath, StringComparison.Ordinal));

            if (isUsed)
            {
                image.BlogPostId = blogPostId;
                image.UpdatedDate = DateTime.UtcNow;
            }
        }
    }

    public async Task<IReadOnlyList<UploadedImage>> GetByBlogPostIdAsync(
        int blogPostId, CancellationToken cancellationToken = default) =>
        await _dbContext.UploadedImages
            .Where(i => i.BlogPostId == blogPostId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UploadedImage>> GetOrphanedByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.UploadedImages
            .Where(i => i.CreatedBy == userId && i.BlogPostId == null)
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<UploadedImage> images) => _dbContext.UploadedImages.RemoveRange(images);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
