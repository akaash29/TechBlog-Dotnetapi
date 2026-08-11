using MediatR;
using UserRegistration.Application.DTOs.Analytics;

namespace UserRegistration.Application.Features.Analytics.TrackPageView;

public sealed class TrackPageViewCommand : IRequest<TrackPageViewResponse>
{
    public string SessionId { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public Guid? UserId { get; set; }
}
