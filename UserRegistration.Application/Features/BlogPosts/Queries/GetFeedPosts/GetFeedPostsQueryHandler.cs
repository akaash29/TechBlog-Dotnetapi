using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetFeedPosts;

public sealed class GetFeedPostsQueryHandler : IRequestHandler<GetFeedPostsQuery, PagedResult<BlogPostSummaryDto>>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetFeedPostsQueryHandler(IBlogPostRepository blogPostRepository, ICurrentUserService currentUserService)
    {
        _blogPostRepository = blogPostRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<BlogPostSummaryDto>> Handle(GetFeedPostsQuery request, CancellationToken cancellationToken)
    {
        var tab = request.Tab.ToLowerInvariant();

        // "foryou" is personalized by the caller's own posting history — an
        // anonymous request (or one from someone who hasn't posted yet)
        // simply gets no boost, not an error; it reads like a warm "trending".
        int? preferredCategoryId = null;
        if (tab == "foryou" && _currentUserService.UserId is { } userId)
        {
            preferredCategoryId = await _blogPostRepository.GetPreferredCategoryIdAsync(userId, cancellationToken);
        }

        var (items, totalCount) = await _blogPostRepository.GetFeedPagedAsync(
            tab, preferredCategoryId, request.Page, request.PageSize, cancellationToken);

        return new PagedResult<BlogPostSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }
}
