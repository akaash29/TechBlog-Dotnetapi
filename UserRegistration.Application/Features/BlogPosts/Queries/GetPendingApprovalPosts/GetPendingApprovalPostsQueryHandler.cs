using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetPendingApprovalPosts;

public sealed class GetPendingApprovalPostsQueryHandler
    : IRequestHandler<GetPendingApprovalPostsQuery, IReadOnlyList<BlogPostSummaryDto>>
{
    private readonly IBlogPostRepository _blogPostRepository;

    public GetPendingApprovalPostsQueryHandler(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository;
    }

    public Task<IReadOnlyList<BlogPostSummaryDto>> Handle(
        GetPendingApprovalPostsQuery request, CancellationToken cancellationToken) =>
        _blogPostRepository.GetPendingApprovalAsync(cancellationToken);
}
