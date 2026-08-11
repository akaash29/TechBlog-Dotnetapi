using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Comments;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Comments.Commands.UpdateComment;

public sealed class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCommentCommandHandler(ICommentRepository commentRepository, ICurrentUserService currentUserService)
    {
        _commentRepository = commentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), request.Id);

        var callerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to edit a comment.");
        var isAdmin = _currentUserService.IsInRole(nameof(UserRole.Admin));

        if (!isAdmin && callerId != comment.CreatedBy)
        {
            throw new ForbiddenException("You can only edit your own comments.");
        }

        comment.CommentText = request.CommentText;
        comment.UpdatedBy = callerId;
        comment.UpdatedDate = DateTime.UtcNow;

        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync(cancellationToken);

        return new CommentDto
        {
            Id = comment.Id,
            BlogPostId = comment.BlogPostId,
            CommentText = comment.CommentText,
            CreatedBy = comment.CreatedBy,
            AuthorName = comment.CreatedByUser.FullName,
            AuthorProfileImagePath = comment.CreatedByUser.ProfileImagePath,
            CreatedDate = comment.CreatedDate,
            UpdatedDate = comment.UpdatedDate
        };
    }
}
