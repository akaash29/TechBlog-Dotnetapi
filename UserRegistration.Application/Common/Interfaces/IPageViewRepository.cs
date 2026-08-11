using UserRegistration.Application.DTOs.Analytics;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface IPageViewRepository
{
    Task AddAsync(PageView pageView, CancellationToken cancellationToken = default);

    Task<PageView?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> CountTotalVisitsAsync(DateTime from, CancellationToken cancellationToken = default);

    Task<int> CountUniqueVisitorsAsync(DateTime from, CancellationToken cancellationToken = default);

    Task<int> CountActiveNowAsync(DateTime since, CancellationToken cancellationToken = default);

    Task<double> GetAverageSessionDurationSecondsAsync(DateTime from, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CountryVisitCount>> GetVisitsByCountryAsync(DateTime from, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyVisitCount>> GetVisitsOverTimeAsync(DateTime from, CancellationToken cancellationToken = default);

    /// <summary>Same day-bucketed shape as GetVisitsOverTimeAsync, scoped to
    /// a specific set of paths — used to compute a single author's own
    /// "reads this week" from the site-wide page-view log.</summary>
    Task<IReadOnlyList<DailyVisitCount>> GetVisitsOverTimeForPathsAsync(
        IReadOnlyList<string> paths, DateTime from, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PageVisitCount>> GetTopPagesAsync(DateTime from, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceVisitCount>> GetVisitsByDeviceAsync(DateTime from, CancellationToken cancellationToken = default);
}
