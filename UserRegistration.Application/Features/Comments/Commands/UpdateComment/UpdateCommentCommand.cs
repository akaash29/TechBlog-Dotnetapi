using MediatR;
using UserRegistration.Application.DTOs.Comments;

namespace UserRegistration.Application.Features.Comments.Commands.UpdateComment;

public sealed class UpdateCommentCommand : IRequest<CommentDto>
{
    public int Id { get; set; }

    public string CommentText { get; set; } = string.Empty;
}
