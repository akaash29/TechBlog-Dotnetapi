using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.BlogPosts.Commands.ApproveBlogPost;

public sealed class ApproveBlogPostCommandHandler : IRequestHandler<ApproveBlogPostCommand, BlogPostDto>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICacheInvalidator _cacheInvalidator;

    public ApproveBlogPostCommandHandler(IBlogPostRepository blogPostRepository, ICacheInvalidator cacheInvalidator)
    {
        _blogPostRepository = blogPostRepository;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<BlogPostDto> Handle(ApproveBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogPostRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BlogPost), request.Id);

        if (post.Status != BlogPostStatus.PendingApproval)
        {
            throw new ConflictException("Only a post awaiting approval can be approved.");
        }

        post.Status = BlogPostStatus.Published;
        post.UpdatedDate = DateTime.UtcNow;
        _blogPostRepository.Update(post);
        await _blogPostRepository.SaveChangesAsync(cancellationToken);

        // The now-published post needs to show up in the feed/journal right
        // away, and drop off the pending-approval list.
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
            CategoryId = post.CategoryId,
            CategoryName = post.Category.Name,
            CreatedBy = post.CreatedBy,
            CreatedDate = post.CreatedDate,
            UpdatedDate = post.UpdatedDate
        };
    }
}
