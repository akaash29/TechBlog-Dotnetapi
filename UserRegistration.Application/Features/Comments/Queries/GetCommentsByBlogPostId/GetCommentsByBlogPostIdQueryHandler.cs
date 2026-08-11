using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Comments;

namespace UserRegistration.Application.Features.Comments.Queries.GetCommentsByBlogPostId;

public sealed class GetCommentsByBlogPostIdQueryHandler
    : IRequestHandler<GetCommentsByBlogPostIdQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;

    public GetCommentsByBlogPostIdQueryHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsByBlogPostIdQuery request, CancellationToken cancellationToken)
    {
        var comments = await _commentRepository.GetByBlogPostIdAsync(request.BlogPostId, cancellationToken);

        return comments.Select(c => new CommentDto
        {
            Id = c.Id,
            BlogPostId = c.BlogPostId,
            CommentText = c.CommentText,
            CreatedBy = c.CreatedBy,
            AuthorName = c.CreatedByUser.FullName,
            AuthorProfileImagePath = c.CreatedByUser.ProfileImagePath,
            CreatedDate = c.CreatedDate,
            UpdatedDate = c.UpdatedDate
        }).ToList();
    }
}
