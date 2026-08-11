using UserRegistration.Domain.Common;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Domain.Entities;

public class BlogPost : AuditableEntity
{
    public string Header { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The post body, authored as HTML by the compose editor.</summary>
    public string PostHtml { get; set; } = string.Empty;

    /// <summary>Blob storage URL of the cover image, if one was uploaded.
    /// Kept directly on the post (like PostHtml) for fast reads; the
    /// underlying upload is still tracked in UploadedImages for audit.</summary>
    public string? CoverImagePath { get; set; }

    /// <summary>Draft / PendingApproval / Published — see BlogPostStatus.</summary>
    public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;

    public int LikesCount { get; set; }

    public int CommentsCount { get; set; }

    public int ViewCount { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public User? UpdatedByUser { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<UploadedImage> Images { get; set; } = new List<UploadedImage>();
}
