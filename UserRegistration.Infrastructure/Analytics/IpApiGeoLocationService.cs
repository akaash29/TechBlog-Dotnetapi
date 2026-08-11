using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UserRegistration.Application.Common.Interfaces;

namespace UserRegistration.Infrastructure.Analytics;

/// <summary>
/// Resolves IP -> country via ip-api.com's free tier (no key, ~45 req/min).
/// Swap this out for a paid/self-hosted GeoIP provider if traffic outgrows
/// that limit — nothing outside this class needs to change.
/// </summary>
public sealed class IpApiGeoLocationService : IGeoLocationService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IpApiGeoLocationService> _logger;

    public IpApiGeoLocationService(HttpClient httpClient, IMemoryCache cache, ILogger<IpApiGeoLocationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetCountryAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out var parsed))
        {
            return null;
        }

        if (IsPrivateOrLoopback(parsed))
        {
            // Every local-dev request looks like this — not worth a network call.
            return null;
        }

        var cacheKey = $"geo:{ipAddress}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(
                $"/json/{ipAddress}?fields=status,country", cancellationToken);

            var country = response is { Status: "success" } ? response.Country : null;
            _cache.Set(cacheKey, country, CacheDuration);
            return country;
        }
        catch (Exception ex)
        {
            // A flaky geolocation lookup should never break page-view tracking.
            _logger.LogWarning(ex, "Geolocation lookup failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                172 => bytes[1] is >= 16 and <= 31,
                192 => bytes[1] == 168,
                _ => false
            };
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
    }

    private sealed class IpApiResponse
    {
        public string? Status { get; set; }

        public string? Country { get; set; }
    }
}
