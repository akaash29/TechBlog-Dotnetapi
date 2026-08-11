using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetSuggestedBlogPosts;

/// <summary>"Strong pieces from outside your own beat" — published posts
/// excluding the caller's own and excluding their preferred/most-posted-in
/// category (that's what the "for you" feed tab already leans into), ranked
/// by likes. Anonymous callers just get the plain top-liked list, same
/// graceful fallback GetFeedPostsQuery uses for "foryou".</summary>
public sealed record GetSuggestedBlogPostsQuery(int Take) : IRequest<IReadOnlyList<BlogPostSummaryDto>>;
