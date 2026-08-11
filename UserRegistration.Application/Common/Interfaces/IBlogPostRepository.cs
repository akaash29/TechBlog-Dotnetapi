using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface IBlogPostRepository
{
    Task AddAsync(BlogPost blogPost, CancellationToken cancellationToken = default);

    Task<BlogPost?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Published posts, tab-ordered, for the feed page.</summary>
    /// <param name="tab">"foryou", "latest", or "trending".</param>
    /// <param name="preferredCategoryId">The signed-in user's most-posted-in
    /// category, used to bias the "foryou" ordering — null falls back to
    /// the same reach/warmth blend "trending" uses.</param>
    Task<(IReadOnlyList<BlogPostSummaryDto> Items, int TotalCount)> GetFeedPagedAsync(
        string tab,
        int? preferredCategoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Published posts, newest first, optionally filtered to one
    /// category, for the journal page.</summary>
    Task<(IReadOnlyList<BlogPostSummaryDto> Items, int TotalCount)> GetJournalPagedAsync(
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Top published posts by a single metric.</summary>
    /// <param name="metric">"views", "likes", or "comments".</param>
    /// <param name="excludeUserId">Skip posts written by this user — used by
    /// "suggested for you" to surface other people's work.</param>
    /// <param name="excludeCategoryId">Skip posts in this category — "suggested
    /// for you" excludes the reader's own preferred/most-posted-in category,
    /// since that's already what the "for you" tab leans into.</param>
    Task<IReadOnlyList<BlogPostSummaryDto>> GetTopAsync(
        string metric,
        int take,
        Guid? excludeUserId = null,
        int? excludeCategoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>The category the user has posted in most often — the "foryou"
    /// feed's stand-in for a beat/follow graph the app doesn't have.</summary>
    Task<int?> GetPreferredCategoryIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task IncrementViewCountAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds or removes the caller's like (whichever the current state
    /// calls for) and returns the resulting state — the source of truth for
    /// both the like count and whether the like button should render "on".</summary>
    Task<(bool Liked, int LikesCount)> ToggleLikeAsync(int blogPostId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Every post id the given user currently has liked — cross-referenced
    /// client-side against whatever page of posts is on screen so the like button
    /// renders correctly highlighted after a reload or a fresh page load.</summary>
    Task<IReadOnlyList<int>> GetLikedPostIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    void Update(BlogPost blogPost);

    /// <summary>Deletes a post outright — used when an admin rejects a
    /// pending one, since there's no persisted "Rejected" state to move it
    /// to instead (see RejectBlogPostCommandHandler).</summary>
    void Remove(BlogPost blogPost);

    /// <summary>Every post awaiting review, oldest first — powers the
    /// admin-only PendingApproval page.</summary>
    Task<IReadOnlyList<BlogPostSummaryDto>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);

    /// <summary>This user's own Published posts: how many, and the totals
    /// (reads/comments/likes) across them — the profile page's stat strip.
    /// Leaves ReadsThisWeek unset; the query handler fills that in from
    /// IPageViewRepository using GetPublishedPostIdsByAuthorAsync below.</summary>
    Task<AuthorStatsDto> GetAuthorStatsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ids of this user's own Published posts — paired with
    /// IPageViewRepository.GetVisitsOverTimeForPathsAsync to compute
    /// "reads this week" from the existing page-view log rather than a new
    /// per-post-per-day counter.</summary>
    Task<IReadOnlyList<int>> GetPublishedPostIdsByAuthorAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
