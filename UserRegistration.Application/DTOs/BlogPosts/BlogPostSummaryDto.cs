namespace UserRegistration.Application.DTOs.BlogPosts;

/// <summary>The shape used everywhere a post appears as a card — feed,
/// journal, "most viewed"/"most liked"/"most discussed" rails.</summary>
public sealed class BlogPostSummaryDto
{
    public int Id { get; set; }

    public string Header { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? CoverImagePath { get; set; }

    /// <summary>Draft / PendingApproval / Published. Feed/journal/top only
    /// ever return Published rows; the pending-approval page is the one
    /// place this varies.</summary>
    public string Status { get; set; } = string.Empty;

    public int LikesCount { get; set; }

    public int CommentsCount { get; set; }

    public int ViewCount { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string? AuthorProfileImagePath { get; set; }

    public DateTime CreatedDate { get; set; }
}
