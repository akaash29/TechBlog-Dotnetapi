namespace UserRegistration.Application.DTOs.BlogPosts;

public sealed class BlogPostDto
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

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
