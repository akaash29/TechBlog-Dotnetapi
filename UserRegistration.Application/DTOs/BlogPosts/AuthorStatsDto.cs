using UserRegistration.Application.DTOs.Analytics;

namespace UserRegistration.Application.DTOs.BlogPosts;

/// <summary>The profile page's stat strip — a signed-in writer's own
/// Published posts, and the engagement they've drawn.</summary>
public sealed class AuthorStatsDto
{
    public int Posts { get; set; }

    /// <summary>Total ViewCount across this author's Published posts.</summary>
    public int Reads { get; set; }

    /// <summary>Total CommentsCount across this author's Published posts.</summary>
    public int Comments { get; set; }

    /// <summary>Total LikesCount across this author's Published posts.</summary>
    public int Likes { get; set; }

    /// <summary>Reads (PageView hits on this author's own /post/{id} pages)
    /// for each of the last 7 days, oldest first. A day with zero reads
    /// still appears with Count = 0 — the sparkline needs a fixed 7 bars.</summary>
    public IReadOnlyList<DailyVisitCount> ReadsThisWeek { get; set; } = Array.Empty<DailyVisitCount>();
}
