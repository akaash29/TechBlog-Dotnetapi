using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetTopBlogPosts;

public sealed class GetTopBlogPostsQuery : IRequest<IReadOnlyList<BlogPostSummaryDto>>
{
    public GetTopBlogPostsQuery(string metric, int take)
    {
        Metric = metric;
        Take = take;
    }

    /// <summary>"views", "likes", or "comments".</summary>
    public string Metric { get; set; }

    public int Take { get; set; }
}
