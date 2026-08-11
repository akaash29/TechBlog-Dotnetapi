using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.BlogPosts.Commands.LikeBlogPost;

public sealed class LikeBlogPostCommandHandler : IRequestHandler<LikeBlogPostCommand, LikeToggleResultDto>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheInvalidator _cacheInvalidator;

    public LikeBlogPostCommandHandler(
        IBlogPostRepository blogPostRepository,
        ICurrentUserService currentUserService,
        ICacheInvalidator cacheInvalidator)
    {
        _blogPostRepository = blogPostRepository;
        _currentUserService = currentUserService;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<LikeToggleResultDto> Handle(LikeBlogPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to like a post.");

        _ = await _blogPostRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BlogPost), request.Id);

        var (liked, likesCount) = await _blogPostRepository.ToggleLikeAsync(request.Id, userId, cancellationToken);

        // So the new count (and the "most liked" ordering) shows up on the
        // next request instead of waiting out the cache's TTL — this is
        // exactly the "click like, count doesn't change" feel we just fixed
        // the underlying bug for; a stale cache would bring it right back.
        await _cacheInvalidator.InvalidateAsync("blogposts", cancellationToken);

        return new LikeToggleResultDto { Liked = liked, LikesCount = likesCount };
    }
}
