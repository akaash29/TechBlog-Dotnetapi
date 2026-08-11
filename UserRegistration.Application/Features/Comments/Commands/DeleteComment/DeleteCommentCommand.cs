using MediatR;

namespace UserRegistration.Application.Features.Comments.Commands.DeleteComment;

public sealed record DeleteCommentCommand(int Id) : IRequest;
