namespace UserRegistration.IntegrationTests.Infrastructure;

/// <summary>Shared setup for every endpoint test class — one API instance and
/// one in-memory database per test class (via IClassFixture), a fresh
/// HttpClient per test method (fresh instance each time xUnit constructs
/// the test class, so per-test auth headers never bleed into another test).</summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
