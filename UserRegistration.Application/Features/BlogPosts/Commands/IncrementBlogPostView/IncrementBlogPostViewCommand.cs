using MediatR;

namespace UserRegistration.Application.Features.BlogPosts.Commands.IncrementBlogPostView;

/// <summary>Best-effort — anonymous readers count too, same as page-view
/// analytics elsewhere in the app. Not tied to an IRequest&lt;T&gt; result
/// beyond confirming the post exists.</summary>
public sealed record IncrementBlogPostViewCommand(int Id) : IRequest;
