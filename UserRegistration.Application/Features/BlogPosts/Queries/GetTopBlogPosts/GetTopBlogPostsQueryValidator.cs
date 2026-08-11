using FluentValidation;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetTopBlogPosts;

public sealed class GetTopBlogPostsQueryValidator : AbstractValidator<GetTopBlogPostsQuery>
{
    private static readonly string[] AllowedMetrics = ["views", "likes", "comments"];

    public GetTopBlogPostsQueryValidator()
    {
        RuleFor(x => x.Metric)
            .Must(metric => AllowedMetrics.Contains(metric, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Metric must be one of: {string.Join(", ", AllowedMetrics)}.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, 20);
    }
}
