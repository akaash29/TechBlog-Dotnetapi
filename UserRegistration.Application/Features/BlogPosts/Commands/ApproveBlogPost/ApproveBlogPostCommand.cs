using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Commands.ApproveBlogPost;

/// <summary>Publishes a pending post. Admin-only — enforced by
/// [Authorize(Roles = Admin)] on the controller action, not here.</summary>
public sealed record ApproveBlogPostCommand(int Id) : IRequest<BlogPostDto>;
