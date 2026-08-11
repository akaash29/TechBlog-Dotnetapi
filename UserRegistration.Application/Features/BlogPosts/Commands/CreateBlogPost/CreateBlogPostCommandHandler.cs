using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.BlogPosts.Commands.CreateBlogPost;

public sealed class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, BlogPostDto>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUploadedImageRepository _uploadedImageRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheInvalidator _cacheInvalidator;

    public CreateBlogPostCommandHandler(
        IBlogPostRepository blogPostRepository,
        ICategoryRepository categoryRepository,
        IUploadedImageRepository uploadedImageRepository,
        ICurrentUserService currentUserService,
        ICacheInvalidator cacheInvalidator)
    {
        _blogPostRepository = blogPostRepository;
        _categoryRepository = categoryRepository;
        _uploadedImageRepository = uploadedImageRepository;
        _currentUserService = currentUserService;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<BlogPostDto> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to save a post.");

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        // "Save draft" always lands in Draft. "Publish" from an admin goes
        // straight to Published; from anyone else it goes to PendingApproval
        // instead — an admin still has to review and approve it (see
        // ApproveBlogPostCommand/PendingApproval page) before it's public.
        var status = request.IsDraft
            ? BlogPostStatus.Draft
            : _currentUserService.IsInRole(nameof(UserRole.Admin))
                ? BlogPostStatus.Published
                : BlogPostStatus.PendingApproval;

        var post = new BlogPost
        {
            // The compose page has no separate "header" field — it mirrors the
            // headline. Kept as its own column since a distinct header/kicker
            // is a reasonable thing to add a dedicated field for later.
            Header = request.Title,
            Title = request.Title,
            Description = request.Description,
            PostHtml = request.PostHtml,
            CoverImagePath = request.CoverImagePath,
            CategoryId = category.Id,
            Status = status,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _blogPostRepository.AddAsync(post, cancellationToken);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        await _uploadedImageRepository.LinkOrphanedImagesAsync(
            userId, post.Id, post.PostHtml, post.CoverImagePath, cancellationToken);
        await _uploadedImageRepository.SaveChangesAsync(cancellationToken);

        // So a freshly-published post shows up in the feed/journal on the very
        // next request instead of waiting out the cache's TTL.
        await _cacheInvalidator.InvalidateAsync("blogposts", cancellationToken);

        return new BlogPostDto
        {
            Id = post.Id,
            Header = post.Header,
            Title = post.Title,
            Description = post.Description,
            PostHtml = post.PostHtml,
            CoverImagePath = post.CoverImagePath,
            Status = post.Status.ToString(),
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            ViewCount = post.ViewCount,
            CategoryId = category.Id,
            CategoryName = category.Name,
            CreatedBy = post.CreatedBy,
            CreatedDate = post.CreatedDate,
            UpdatedDate = post.UpdatedDate
        };
    }
}
