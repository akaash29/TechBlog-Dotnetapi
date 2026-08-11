using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetAuthorStats;

/// <summary>The signed-in caller's own post/reads/comments/likes totals plus
/// a 7-day reads breakdown, for the profile page's stat strip.</summary>
public sealed record GetAuthorStatsQuery : IRequest<AuthorStatsDto>;
