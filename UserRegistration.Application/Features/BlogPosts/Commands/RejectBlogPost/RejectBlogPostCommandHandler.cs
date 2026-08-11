using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.BlogPosts.Commands.RejectBlogPost;

public sealed class RejectBlogPostCommandHandler : IRequestHandler<RejectBlogPostCommand>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IUploadedImageRepository _uploadedImageRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICacheInvalidator _cacheInvalidator;

    public RejectBlogPostCommandHandler(
        IBlogPostRepository blogPostRepository,
        IUploadedImageRepository uploadedImageRepository,
        IBlobStorageService blobStorageService,
        ICacheInvalidator cacheInvalidator)
    {
        _blogPostRepository = blogPostRepository;
        _uploadedImageRepository = uploadedImageRepository;
        _blobStorageService = blobStorageService;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task Handle(RejectBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogPostRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BlogPost), request.Id);

        if (post.Status != BlogPostStatus.PendingApproval)
        {
            throw new ConflictException("Only a post awaiting approval can be rejected.");
        }

        var images = await _uploadedImageRepository.GetByBlogPostIdAsync(post.Id, cancellationToken);

        // Blob deletes happen before the DB delete: if a blob delete fails
        // partway through, the post (and the still-undeleted images' rows)
        // stay around for a retry instead of the DB losing track of files
        // that are still sitting in storage.
        foreach (var image in images)
        {
            await _blobStorageService.DeleteAsync(image.ImagePath, cancellationToken);
        }
        if (post.CoverImagePath is { } coverImagePath &&
            !images.Any(i => i.ImagePath == coverImagePath))
        {
            await _blobStorageService.DeleteAsync(coverImagePath, cancellationToken);
        }

        _uploadedImageRepository.RemoveRange(images);
        await _uploadedImageRepository.SaveChangesAsync(cancellationToken);

        _blogPostRepository.Remove(post);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateAsync("blogposts", cancellationToken);
    }
}
