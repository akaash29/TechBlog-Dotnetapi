namespace UserRegistration.Domain.Entities;

/// <summary>
/// One row per (user, post) like — the source of truth backing BlogPost.LikesCount.
/// Toggling a like means adding/removing a row here (and adjusting the count to
/// match), rather than just incrementing a bare counter: without this, there's no
/// way to know a given user already liked a post, so a second click can't turn
/// into an "unlike" and a page reload can't restore the button's highlighted state.
/// </summary>
public class PostLike
{
    public int Id { get; set; }

    public int BlogPostId { get; set; }

    public BlogPost BlogPost { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
