namespace UserRegistration.Application.Common.Interfaces;

public interface IGeoLocationService
{
    /// <summary>Resolves an IP address to a country name, or null if it can't
    /// be determined (local/private address, or the lookup failed).</summary>
    Task<string?> GetCountryAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
