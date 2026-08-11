namespace UserRegistration.Application.DTOs.Comments;

public sealed class CommentDto
{
    public int Id { get; set; }

    public int BlogPostId { get; set; }

    public string CommentText { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string? AuthorProfileImagePath { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
