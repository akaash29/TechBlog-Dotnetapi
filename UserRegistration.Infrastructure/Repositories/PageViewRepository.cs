using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Analytics;
using UserRegistration.Domain.Entities;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class PageViewRepository : IPageViewRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PageViewRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PageView pageView, CancellationToken cancellationToken = default) =>
        await _dbContext.PageViews.AddAsync(pageView, cancellationToken);

    public Task<PageView?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _dbContext.PageViews.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public Task<int> CountTotalVisitsAsync(DateTime from, CancellationToken cancellationToken = default) =>
        _dbContext.PageViews.CountAsync(v => v.VisitedAt >= from, cancellationToken);

    public Task<int> CountUniqueVisitorsAsync(DateTime from, CancellationToken cancellationToken = default) =>
        _dbContext.PageViews
            .Where(v => v.VisitedAt >= from)
            .Select(v => v.SessionId)
            .Distinct()
            .CountAsync(cancellationToken);

    public Task<int> CountActiveNowAsync(DateTime since, CancellationToken cancellationToken = default) =>
        _dbContext.PageViews
            .Where(v => v.LastActivityAt >= since)
            .Select(v => v.SessionId)
            .Distinct()
            .CountAsync(cancellationToken);

    public async Task<double> GetAverageSessionDurationSecondsAsync(DateTime from, CancellationToken cancellationToken = default)
    {
        // "A session's duration" = the sum of every page view it made (a
        // visitor reading three pages for a minute each spent three minutes,
        // not one) — then averaged across sessions.
        var perSessionTotals = await _dbContext.PageViews
            .Where(v => v.VisitedAt >= from)
            .GroupBy(v => v.SessionId)
            .Select(g => g.Sum(v => v.DurationSeconds))
            .ToListAsync(cancellationToken);

        return perSessionTotals.Count > 0 ? perSessionTotals.Average() : 0;
    }

    public Task<IReadOnlyList<CountryVisitCount>> GetVisitsByCountryAsync(DateTime from, CancellationToken cancellationToken = default) =>
        MaterializeAsync(
            _dbContext.PageViews
                .Where(v => v.VisitedAt >= from && v.Country != null)
                .GroupBy(v => v.Country!)
                // Order by the grouping's own count before projecting into the
                // record — ordering by a property of the newly-constructed DTO
                // (g => new CountryVisitCount(...)).Count) doesn't translate.
                .OrderByDescending(g => g.Count())
                .Select(g => new CountryVisitCount(g.Key, g.Count())),
            cancellationToken);

    public async Task<IReadOnlyList<DailyVisitCount>> GetVisitsOverTimeAsync(DateTime from, CancellationToken cancellationToken = default)
    {
        var raw = await _dbContext.PageViews
            .Where(v => v.VisitedAt >= from)
            .GroupBy(v => v.VisitedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync(cancellationToken);

        return raw.Select(r => new DailyVisitCount(DateOnly.FromDateTime(r.Date), r.Count)).ToList();
    }

    public async Task<IReadOnlyList<DailyVisitCount>> GetVisitsOverTimeForPathsAsync(
        IReadOnlyList<string> paths, DateTime from, CancellationToken cancellationToken = default)
    {
        if (paths.Count == 0)
        {
            return Array.Empty<DailyVisitCount>();
        }

        var raw = await _dbContext.PageViews
            .Where(v => v.VisitedAt >= from && paths.Contains(v.Path))
            .GroupBy(v => v.VisitedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync(cancellationToken);

        return raw.Select(r => new DailyVisitCount(DateOnly.FromDateTime(r.Date), r.Count)).ToList();
    }

    public Task<IReadOnlyList<PageVisitCount>> GetTopPagesAsync(DateTime from, int take, CancellationToken cancellationToken = default) =>
        MaterializeAsync(
            _dbContext.PageViews
                .Where(v => v.VisitedAt >= from)
                .GroupBy(v => v.Path)
                .OrderByDescending(g => g.Count())
                .Select(g => new PageVisitCount(g.Key, g.Count()))
                .Take(take),
            cancellationToken);

    public Task<IReadOnlyList<DeviceVisitCount>> GetVisitsByDeviceAsync(DateTime from, CancellationToken cancellationToken = default) =>
        MaterializeAsync(
            _dbContext.PageViews
                .Where(v => v.VisitedAt >= from)
                .GroupBy(v => v.DeviceType)
                .OrderByDescending(g => g.Count())
                .Select(g => new DeviceVisitCount(g.Key, g.Count())),
            cancellationToken);

    private static async Task<IReadOnlyList<T>> MaterializeAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
        await query.ToListAsync(cancellationToken);
}
