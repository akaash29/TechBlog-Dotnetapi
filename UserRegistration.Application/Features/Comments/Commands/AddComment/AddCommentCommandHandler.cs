using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Comments;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.Comments.Commands.AddComment;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheInvalidator _cacheInvalidator;

    public AddCommentCommandHandler(
        ICommentRepository commentRepository,
        IBlogPostRepository blogPostRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ICacheInvalidator cacheInvalidator)
    {
        _commentRepository = commentRepository;
        _blogPostRepository = blogPostRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to comment.");

        var post = await _blogPostRepository.GetByIdAsync(request.BlogPostId, cancellationToken)
            ?? throw new NotFoundException(nameof(BlogPost), request.BlogPostId);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        var comment = new Comment
        {
            BlogPostId = post.Id,
            CommentText = request.CommentText,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment, cancellationToken);

        // Same DbContext as the comment insert — one SaveChanges persists
        // both atomically, so the count can't drift from the actual rows.
        post.CommentsCount++;
        _blogPostRepository.Update(post);

        await _commentRepository.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync("blogposts", cancellationToken);

        return new CommentDto
        {
            Id = comment.Id,
            BlogPostId = comment.BlogPostId,
            CommentText = comment.CommentText,
            CreatedBy = comment.CreatedBy,
            AuthorName = user.FullName,
            AuthorProfileImagePath = user.ProfileImagePath,
            CreatedDate = comment.CreatedDate,
            UpdatedDate = comment.UpdatedDate
        };
    }
}
