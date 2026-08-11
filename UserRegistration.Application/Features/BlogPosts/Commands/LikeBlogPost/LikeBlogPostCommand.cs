using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Commands.LikeBlogPost;

/// <summary>Toggles the caller's like on a post — a second call from the same
/// user un-likes it. Backed by a per-(post, user) row (see PostLike), not
/// just a bare counter, so the state survives a reload and can't be
/// double-counted.</summary>
public sealed record LikeBlogPostCommand(int Id) : IRequest<LikeToggleResultDto>;
