using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetTopBlogPosts;

public sealed class GetTopBlogPostsQueryHandler : IRequestHandler<GetTopBlogPostsQuery, IReadOnlyList<BlogPostSummaryDto>>
{
    private readonly IBlogPostRepository _blogPostRepository;

    public GetTopBlogPostsQueryHandler(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository;
    }

    public Task<IReadOnlyList<BlogPostSummaryDto>> Handle(GetTopBlogPostsQuery request, CancellationToken cancellationToken) =>
        _blogPostRepository.GetTopAsync(request.Metric.ToLowerInvariant(), request.Take, cancellationToken: cancellationToken);
}
