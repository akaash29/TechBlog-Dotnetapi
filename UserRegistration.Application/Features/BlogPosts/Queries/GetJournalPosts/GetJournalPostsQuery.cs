using MediatR;
using UserRegistration.Application.Common.Models;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetJournalPosts;

public sealed class GetJournalPostsQuery : IRequest<PagedResult<BlogPostSummaryDto>>
{
    public GetJournalPostsQuery(int? categoryId, int page, int pageSize)
    {
        CategoryId = categoryId;
        Page = page;
        PageSize = pageSize;
    }

    public int? CategoryId { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
