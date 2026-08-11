namespace UserRegistration.Application.Common.Interfaces;

/// <summary>Evicts cached GET responses tagged with the given tag (see
/// Program.cs's output-cache policies) — called by command handlers whose
/// writes would otherwise sit behind a stale cached read until its TTL
/// expires.</summary>
public interface ICacheInvalidator
{
    Task InvalidateAsync(string tag, CancellationToken cancellationToken = default);
}
