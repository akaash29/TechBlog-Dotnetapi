namespace UserRegistration.Application.DTOs.BlogPosts;

/// <summary>The full post, as read on the post page — everything in
/// BlogPostSummaryDto plus the body and draft/update bookkeeping.</summary>
public sealed class BlogPostDetailDto
{
    public int Id { get; set; }

    public string Header { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PostHtml { get; set; } = string.Empty;

    public string? CoverImagePath { get; set; }

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

    public DateTime? UpdatedDate { get; set; }
}
