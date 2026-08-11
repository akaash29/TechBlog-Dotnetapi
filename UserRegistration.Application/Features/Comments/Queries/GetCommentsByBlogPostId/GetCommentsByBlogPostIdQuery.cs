using MediatR;
using UserRegistration.Application.DTOs.Comments;

namespace UserRegistration.Application.Features.Comments.Queries.GetCommentsByBlogPostId;

public sealed record GetCommentsByBlogPostIdQuery(int BlogPostId) : IRequest<IReadOnlyList<CommentDto>>;
