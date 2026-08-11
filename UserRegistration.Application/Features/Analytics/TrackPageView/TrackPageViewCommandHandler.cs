using MediatR;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Analytics;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.Analytics.TrackPageView;

public sealed class TrackPageViewCommandHandler : IRequestHandler<TrackPageViewCommand, TrackPageViewResponse>
{
    private readonly IPageViewRepository _pageViewRepository;
    private readonly IGeoLocationService _geoLocationService;

    public TrackPageViewCommandHandler(
        IPageViewRepository pageViewRepository,
        IGeoLocationService geoLocationService)
    {
        _pageViewRepository = pageViewRepository;
        _geoLocationService = geoLocationService;
    }

    public async Task<TrackPageViewResponse> Handle(TrackPageViewCommand request, CancellationToken cancellationToken)
    {
        var country = await _geoLocationService.GetCountryAsync(request.IpAddress, cancellationToken);
        var now = DateTime.UtcNow;

        var pageView = new PageView
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            Path = request.Path,
            Country = country,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            DeviceType = DetectDeviceType(request.UserAgent),
            VisitedAt = now,
            LastActivityAt = now
        };

        await _pageViewRepository.AddAsync(pageView, cancellationToken);
        await _pageViewRepository.SaveChangesAsync(cancellationToken);

        return new TrackPageViewResponse { Id = pageView.Id };
    }

    private static string DetectDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("bot") || ua.Contains("spider") || ua.Contains("crawl")) return "Bot";
        if (ua.Contains("ipad") || ua.Contains("tablet")) return "Tablet";
        if (ua.Contains("mobi") || ua.Contains("android") || ua.Contains("iphone")) return "Mobile";
        return "Desktop";
    }
}
