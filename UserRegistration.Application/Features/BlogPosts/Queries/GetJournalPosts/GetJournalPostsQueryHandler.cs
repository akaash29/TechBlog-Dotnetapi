using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetJournalPosts;

public sealed class GetJournalPostsQueryHandler : IRequestHandler<GetJournalPostsQuery, PagedResult<BlogPostSummaryDto>>
{
    private readonly IBlogPostRepository _blogPostRepository;

    public GetJournalPostsQueryHandler(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository;
    }

    public async Task<PagedResult<BlogPostSummaryDto>> Handle(GetJournalPostsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _blogPostRepository.GetJournalPagedAsync(
            request.CategoryId, request.Page, request.PageSize, cancellationToken);

        return new PagedResult<BlogPostSummaryDto>(items, request.Page, request.PageSize, totalCount);
    }
}
