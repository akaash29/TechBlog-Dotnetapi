namespace UserRegistration.Application.DTOs.Analytics;

public sealed record CountryVisitCount(string Country, int Count);

public sealed record DailyVisitCount(DateOnly Date, int Count);

public sealed record PageVisitCount(string Path, int Count);

public sealed record DeviceVisitCount(string Device, int Count);

public sealed class AnalyticsSummaryDto
{
    public int TotalVisits { get; set; }

    public int UniqueVisitors { get; set; }

    /// <summary>Distinct sessions with a heartbeat in the last 5 minutes.</summary>
    public int ActiveNow { get; set; }

    public double AverageSessionDurationSeconds { get; set; }

    public IReadOnlyList<CountryVisitCount> VisitsByCountry { get; set; } = [];

    public IReadOnlyList<DailyVisitCount> VisitsOverTime { get; set; } = [];

    public IReadOnlyList<PageVisitCount> TopPages { get; set; } = [];

    public IReadOnlyList<DeviceVisitCount> VisitsByDevice { get; set; } = [];
}
