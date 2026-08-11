using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Commands.CreateBlogPost;

public sealed class CreateBlogPostCommand : IRequest<BlogPostDto>
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PostHtml { get; set; } = string.Empty;

    public string? CoverImagePath { get; set; }

    public int CategoryId { get; set; }

    public bool IsDraft { get; set; }
}
