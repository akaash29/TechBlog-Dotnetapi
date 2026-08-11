namespace UserRegistration.Application.DTOs.BlogPosts;

public sealed class CreateBlogPostRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PostHtml { get; set; } = string.Empty;

    public string? CoverImagePath { get; set; }

    public int CategoryId { get; set; }

    /// <summary>True from "Save draft", false from "Publish".</summary>
    public bool IsDraft { get; set; }
}
