using MediatR;
using UserRegistration.Application.DTOs.Comments;

namespace UserRegistration.Application.Features.Comments.Commands.AddComment;

public sealed class AddCommentCommand : IRequest<CommentDto>
{
    public int BlogPostId { get; set; }

    public string CommentText { get; set; } = string.Empty;
}
