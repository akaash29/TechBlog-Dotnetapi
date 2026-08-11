using MediatR;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetLikedPostIds;

public sealed class GetLikedPostIdsQueryHandler : IRequestHandler<GetLikedPostIdsQuery, IReadOnlyList<int>>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetLikedPostIdsQueryHandler(IBlogPostRepository blogPostRepository, ICurrentUserService currentUserService)
    {
        _blogPostRepository = blogPostRepository;
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<int>> Handle(GetLikedPostIdsQuery request, CancellationToken cancellationToken) =>
        _currentUserService.UserId is { } userId
            ? _blogPostRepository.GetLikedPostIdsAsync(userId, cancellationToken)
            : Task.FromResult<IReadOnlyList<int>>([]);
}
