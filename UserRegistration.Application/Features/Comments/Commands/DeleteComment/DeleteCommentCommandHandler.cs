using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Comments.Commands.DeleteComment;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheInvalidator _cacheInvalidator;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        IBlogPostRepository blogPostRepository,
        ICurrentUserService currentUserService,
        ICacheInvalidator cacheInvalidator)
    {
        _commentRepository = commentRepository;
        _blogPostRepository = blogPostRepository;
        _currentUserService = currentUserService;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), request.Id);

        var callerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to delete a comment.");
        var isAdmin = _currentUserService.IsInRole(nameof(UserRole.Admin));

        if (!isAdmin && callerId != comment.CreatedBy)
        {
            throw new ForbiddenException("You can only delete your own comments.");
        }

        _commentRepository.Remove(comment);

        // Same DbContext as the comment delete — one SaveChanges keeps the
        // count from drifting from the actual rows, same as AddComment.
        var post = await _blogPostRepository.GetByIdAsync(comment.BlogPostId, cancellationToken);
        if (post is not null && post.CommentsCount > 0)
        {
            post.CommentsCount--;
            _blogPostRepository.Update(post);
        }

        await _commentRepository.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync("blogposts", cancellationToken);
    }
}
