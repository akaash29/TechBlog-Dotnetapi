using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetBlogPostById;

public sealed record GetBlogPostByIdQuery(int Id) : IRequest<BlogPostDetailDto>;
