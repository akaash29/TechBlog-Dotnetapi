using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetSuggestedBlogPosts;

public sealed class GetSuggestedBlogPostsQueryHandler
    : IRequestHandler<GetSuggestedBlogPostsQuery, IReadOnlyList<BlogPostSummaryDto>>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSuggestedBlogPostsQueryHandler(IBlogPostRepository blogPostRepository, ICurrentUserService currentUserService)
    {
        _blogPostRepository = blogPostRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<BlogPostSummaryDto>> Handle(GetSuggestedBlogPostsQuery request, CancellationToken cancellationToken)
    {
        Guid? userId = _currentUserService.UserId;
        int? preferredCategoryId = userId is { } id
            ? await _blogPostRepository.GetPreferredCategoryIdAsync(id, cancellationToken)
            : null;

        return await _blogPostRepository.GetTopAsync(
            "likes", request.Take, excludeUserId: userId, excludeCategoryId: preferredCategoryId, cancellationToken);
    }
}
