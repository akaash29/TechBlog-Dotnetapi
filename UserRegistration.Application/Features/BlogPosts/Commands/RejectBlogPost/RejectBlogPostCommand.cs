using MediatR;

namespace UserRegistration.Application.Features.BlogPosts.Commands.RejectBlogPost;

/// <summary>Rejects a pending post: deletes the post record and every image
/// (cover + inline body) it uploaded to blob storage. There's no persisted
/// "Rejected" state — the writer sees it gone, same as if it never existed.
/// Admin-only — enforced by [Authorize(Roles = Admin)] on the controller
/// action, not here.</summary>
public sealed record RejectBlogPostCommand(int Id) : IRequest;
