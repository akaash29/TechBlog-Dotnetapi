using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Analytics;

namespace UserRegistration.Application.Features.Analytics.GetAnalyticsSummary;

public sealed class GetAnalyticsSummaryQueryHandler : IRequestHandler<GetAnalyticsSummaryQuery, AnalyticsSummaryDto>
{
    private const int TopPagesLimit = 8;
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(5);

    private readonly IPageViewRepository _pageViewRepository;

    public GetAnalyticsSummaryQueryHandler(IPageViewRepository pageViewRepository)
    {
        _pageViewRepository = pageViewRepository;
    }

    public async Task<AnalyticsSummaryDto> Handle(GetAnalyticsSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var from = ResolveRangeStart(request.Range, now);

        var totalVisits = await _pageViewRepository.CountTotalVisitsAsync(from, cancellationToken);
        var uniqueVisitors = await _pageViewRepository.CountUniqueVisitorsAsync(from, cancellationToken);
        var activeNow = await _pageViewRepository.CountActiveNowAsync(now - ActiveWindow, cancellationToken);
        var avgDuration = await _pageViewRepository.GetAverageSessionDurationSecondsAsync(from, cancellationToken);
        var byCountry = await _pageViewRepository.GetVisitsByCountryAsync(from, cancellationToken);
        var overTime = await _pageViewRepository.GetVisitsOverTimeAsync(from, cancellationToken);
        var topPages = await _pageViewRepository.GetTopPagesAsync(from, TopPagesLimit, cancellationToken);
        var byDevice = await _pageViewRepository.GetVisitsByDeviceAsync(from, cancellationToken);

        return new AnalyticsSummaryDto
        {
            TotalVisits = totalVisits,
            UniqueVisitors = uniqueVisitors,
            ActiveNow = activeNow,
            AverageSessionDurationSeconds = avgDuration,
            VisitsByCountry = byCountry,
            VisitsOverTime = overTime,
            TopPages = topPages,
            VisitsByDevice = byDevice
        };
    }

    private static DateTime ResolveRangeStart(string range, DateTime now) => range.ToLowerInvariant() switch
    {
        "day" => now.Date,
        "month" => now.AddDays(-30),
        "year" => now.AddDays(-365),
        _ => now.AddDays(-7) // "week" and anything unrecognized
    };
}
