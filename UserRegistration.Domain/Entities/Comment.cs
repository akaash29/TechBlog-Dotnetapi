using UserRegistration.Domain.Common;

namespace UserRegistration.Domain.Entities;

public class Comment : AuditableEntity
{
    /// <summary>Named CommentText rather than Comment — a member can't
    /// share its enclosing type's name in C#.</summary>
    public string CommentText { get; set; } = string.Empty;

    public int BlogPostId { get; set; }

    public BlogPost BlogPost { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public User? UpdatedByUser { get; set; }
}
