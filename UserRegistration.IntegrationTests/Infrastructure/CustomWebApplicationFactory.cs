using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API in-process against an isolated SQLite in-memory
/// database instead of SQL Server, and swaps the two external-service
/// dependencies (Azure Blob Storage, IP geolocation) for fakes — nothing
/// in CI needs a real database, Azurite, or internet access to run these.
///
/// The SQLite connection is opened once and kept open for the factory's
/// lifetime (closing it would drop the in-memory database), so every test
/// in a class sharing one factory instance (via IClassFixture) sees the
/// same database — tests use randomized emails/usernames to avoid
/// colliding with each other rather than resetting state between tests.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Overrides rather than relying on appsettings.json being copied
            // into this project's output — keeps the tests self-contained.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "integration-test-signing-key-please-ignore-32bytes",
                ["Jwt:Issuer"] = "UserRegistration.Tests",
                ["Jwt:Audience"] = "UserRegistration.Tests.Client",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Cors:AllowedOrigins:0"] = "http://localhost",
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddInfrastructureServices already registered SQL Server's provider
            // into this container. Removing just DbContextOptions<ApplicationDbContext>
            // isn't enough — EF Core 8+ tracks a provider's configuration through
            // several descriptors, not only that one — so having SQL Server's and
            // SQLite's both partially registered trips EF's "only one provider"
            // check. Strip every EF Core-related descriptor first and re-add
            // fresh, rather than trying to name each internal type precisely.
            var efCoreDescriptors = services
                .Where(d => d.ServiceType.Assembly.GetName().Name is { } name
                    && (name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Microsoft.Data.Sqlite")))
                .ToList();

            foreach (var descriptor in efCoreDescriptors)
            {
                services.Remove(descriptor);
            }

            _connection.Open();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();

            services.RemoveAll<IGeoLocationService>();
            services.AddSingleton<IGeoLocationService, FakeGeoLocationService>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // EnsureCreated (not Migrate) — migrations can carry SQL Server-only
            // syntax; this builds the schema straight from the current model,
            // including HasData seeds (Roles, Categories), which SQLite can run.
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
