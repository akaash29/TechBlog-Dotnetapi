using MediatR;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Application.Features.Analytics.RecordHeartbeat;

public sealed class RecordHeartbeatCommandHandler : IRequestHandler<RecordHeartbeatCommand>
{
    private readonly IPageViewRepository _pageViewRepository;

    public RecordHeartbeatCommandHandler(IPageViewRepository pageViewRepository)
    {
        _pageViewRepository = pageViewRepository;
    }

    public async Task Handle(RecordHeartbeatCommand request, CancellationToken cancellationToken)
    {
        var pageView = await _pageViewRepository.GetByIdAsync(request.PageViewId, cancellationToken);
        // A missing/stale id (tampered, or the row aged out of a future retention
        // job) isn't worth failing a best-effort background beacon call over.
        if (pageView is null) return;

        pageView.LastActivityAt = DateTime.UtcNow;
        pageView.DurationSeconds = Math.Max(pageView.DurationSeconds, request.ElapsedSeconds);

        await _pageViewRepository.SaveChangesAsync(cancellationToken);
    }
}
