using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.BlogPosts.Commands.IncrementBlogPostView;

public sealed class IncrementBlogPostViewCommandHandler : IRequestHandler<IncrementBlogPostViewCommand>
{
    private readonly IBlogPostRepository _blogPostRepository;

    public IncrementBlogPostViewCommandHandler(IBlogPostRepository blogPostRepository)
    {
        _blogPostRepository = blogPostRepository;
    }

    public async Task Handle(IncrementBlogPostViewCommand request, CancellationToken cancellationToken)
    {
        var post = await _blogPostRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BlogPost), request.Id);

        await _blogPostRepository.IncrementViewCountAsync(post.Id, cancellationToken);
    }
}
