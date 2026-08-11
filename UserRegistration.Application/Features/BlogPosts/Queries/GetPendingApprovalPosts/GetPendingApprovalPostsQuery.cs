using MediatR;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetPendingApprovalPosts;

/// <summary>Every post awaiting review, oldest first — the PendingApproval
/// page's post list. Controller-gated to Admin (see BlogPostsController).</summary>
public sealed record GetPendingApprovalPostsQuery : IRequest<IReadOnlyList<BlogPostSummaryDto>>;
