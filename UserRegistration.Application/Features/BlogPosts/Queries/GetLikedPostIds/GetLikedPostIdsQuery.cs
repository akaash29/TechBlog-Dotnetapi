using MediatR;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetLikedPostIds;

/// <summary>Every post the signed-in caller currently has liked — the client
/// cross-references this against whatever page of posts is on screen to
/// restore the like button's highlighted state after a reload. Anonymous
/// callers just get an empty list back rather than a 401: browsing without
/// being signed in is fine, it's only /like itself that requires auth.</summary>
public sealed record GetLikedPostIdsQuery : IRequest<IReadOnlyList<int>>;
