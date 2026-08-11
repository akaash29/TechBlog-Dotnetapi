using MediatR;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Analytics;
using UserRegistration.Application.DTOs.BlogPosts;

namespace UserRegistration.Application.Features.BlogPosts.Queries.GetAuthorStats;

public sealed class GetAuthorStatsQueryHandler : IRequestHandler<GetAuthorStatsQuery, AuthorStatsDto>
{
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly IPageViewRepository _pageViewRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAuthorStatsQueryHandler(
        IBlogPostRepository blogPostRepository,
        IPageViewRepository pageViewRepository,
        ICurrentUserService currentUserService)
    {
        _blogPostRepository = blogPostRepository;
        _pageViewRepository = pageViewRepository;
        _currentUserService = currentUserService;
    }

    public async Task<AuthorStatsDto> Handle(GetAuthorStatsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your stats.");

        var stats = await _blogPostRepository.GetAuthorStatsAsync(userId, cancellationToken);

        var postIds = await _blogPostRepository.GetPublishedPostIdsByAuthorAsync(userId, cancellationToken);
        var paths = postIds.Select(id => $"/post/{id}").ToList();

        var today = DateTime.UtcNow.Date;
        // 7 days including today.
        var from = today.AddDays(-6);
        var daily = await _pageViewRepository.GetVisitsOverTimeForPathsAsync(paths, from, cancellationToken);
        var byDate = daily.ToDictionary(d => d.Date);

        // The sparkline needs a fixed 7 bars, oldest first — a day with no
        // reads at all still needs a bar, at Count = 0.
        var readsThisWeek = new List<DailyVisitCount>(7);
        for (var i = 0; i < 7; i++)
        {
            var date = DateOnly.FromDateTime(from.AddDays(i));
            readsThisWeek.Add(byDate.TryGetValue(date, out var found) ? found : new DailyVisitCount(date, 0));
        }

        stats.ReadsThisWeek = readsThisWeek;
        return stats;
    }
}
