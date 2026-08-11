using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetBlogPostById;

public sealed class GetBlogPostByIdQueryHandler : IRequestHandler<GetBlogPostByIdQuery, BlogPostDetailDto>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetBlogPostByIdQueryHandler(IBlogPostRepository blogPostRepository, ICurrentUserService currentUserService)
    {
        _blogPostRepository = blogPostRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BlogPostDetailDto> Handle(GetBlogPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await _blogPostRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.BlogPost), request.Id);

        // Only Published posts are public — a Draft or PendingApproval post
        // is visible only to its own author or an admin. Treated as "not
        // found" rather than "forbidden" so a guessed id doesn't confirm a
        // post exists to someone who can't see it.
        if (post.Status != BlogPostStatus.Published)
        {
            var callerId = _currentUserService.UserId;
            var isOwner = callerId == post.CreatedBy;
            var isAdmin = _currentUserService.IsInRole(nameof(UserRole.Admin));
            if (!isOwner && !isAdmin)
            {
                throw new NotFoundException(nameof(Domain.Entities.BlogPost), request.Id);
            }
        }

        return new BlogPostDetailDto
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
            AuthorName = post.CreatedByUser.FullName,
            AuthorProfileImagePath = post.CreatedByUser.ProfileImagePath,
            CreatedDate = post.CreatedDate,
            UpdatedDate = post.UpdatedDate
        };
    }
}
