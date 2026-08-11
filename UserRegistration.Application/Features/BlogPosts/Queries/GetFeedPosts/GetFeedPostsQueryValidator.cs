using FluentValidation;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetFeedPosts;

public sealed class GetFeedPostsQueryValidator : AbstractValidator<GetFeedPostsQuery>
{
    private static readonly string[] AllowedTabs = ["foryou", "latest", "trending"];

    public GetFeedPostsQueryValidator()
    {
        RuleFor(x => x.Tab)
            .Must(tab => AllowedTabs.Contains(tab, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Tab must be one of: {string.Join(", ", AllowedTabs)}.");

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50);
    }
}
