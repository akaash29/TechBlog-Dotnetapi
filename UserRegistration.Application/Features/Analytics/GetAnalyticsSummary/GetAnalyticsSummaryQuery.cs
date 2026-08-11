using MediatR;
using UserRegistration.Application.DTOs.Analytics;

namespace UserRegistration.Application.Features.Analytics.GetAnalyticsSummary;

public sealed class GetAnalyticsSummaryQuery : IRequest<AnalyticsSummaryDto>
{
    /// <summary>day | week | month | year — defaults to week.</summary>
    public string Range { get; set; } = "week";
}
