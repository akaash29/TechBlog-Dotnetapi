using MediatR;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetFeedPosts;

public sealed class GetFeedPostsQuery : IRequest<PagedResult<BlogPostSummaryDto>>
{
    public GetFeedPostsQuery(string tab, int page, int pageSize)
    {
        Tab = tab;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>"foryou", "latest", or "trending".</summary>
    public string Tab { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
