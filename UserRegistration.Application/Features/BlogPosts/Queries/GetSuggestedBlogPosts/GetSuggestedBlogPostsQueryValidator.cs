using FluentValidation;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetSuggestedBlogPosts;

public sealed class GetSuggestedBlogPostsQueryValidator : AbstractValidator<GetSuggestedBlogPostsQuery>
{
    public GetSuggestedBlogPostsQueryValidator()
    {
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 20);
    }
}
