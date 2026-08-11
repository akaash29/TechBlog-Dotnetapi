using UserRegistration.Domain.Common;

namespace UserRegistration.Domain.Entities;

/// <summary>
/// Audit record for every image uploaded through the compose editor (cover
/// or inline body image), independent of whether it ended up used in a
/// post. BlogPostId starts null — an image is uploaded before the post it
/// belongs to is ever saved — and gets linked once the post is created.
/// </summary>
public class UploadedImage : AuditableEntity
{
    public int? BlogPostId { get; set; }

    public BlogPost? BlogPost { get; set; }

    /// <summary>The blob's public URL.</summary>
    public string ImagePath { get; set; } = string.Empty;

    public User CreatedByUser { get; set; } = null!;

    public User? UpdatedByUser { get; set; }
}
