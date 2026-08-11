using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.IntegrationTests.Infrastructure;

/// <summary>Stands in for the real ip-api.com lookup — tests shouldn't
/// depend on outbound internet access or be flaky on its rate limit.</summary>
public sealed class FakeGeoLocationService : IGeoLocationService
{
    public Task<string?> GetCountryAsync(string? ipAddress, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>("Testland");
}
