using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.IntegrationTests.Infrastructure;

public sealed record TestAccount(string Email, string UserName, string Password);

/// <summary>Response shapes as the API actually serializes them (camelCase) —
/// kept minimal, just what the test helpers need to read.</summary>
public sealed class AuthResponseBody
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Registration/login helpers shared by every endpoint test class.
/// Every account uses a random suffix so tests sharing one factory's
/// database don't collide with each other.</summary>
public static class TestAuth
{
    public static async Task<TestAccount> RegisterAsync(
        HttpClient client, string role = "Writer", string? password = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var account = new TestAccount($"user{suffix}@test.local", $"user{suffix}", password ?? "Passw0rd1");

        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            UserName = account.UserName,
            FirstName = "Test",
            LastName = "User",
            Email = account.Email,
            Role = role,
            Password = account.Password
        });
        response.EnsureSuccessStatusCode();

        return account;
    }

    public static async Task<AuthResponseBody> LoginAsync(HttpClient client, TestAccount account)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            EmailOrUserName = account.Email,
            Password = account.Password
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthResponseBody>())!;
    }

    /// <summary>Registers, logs in, and attaches the access token to the given
    /// client's default headers — the common case for "any signed-in user".</summary>
    public static async Task<TestAccount> RegisterAndAuthenticateAsync(
        HttpClient client, string role = "Writer")
    {
        var account = await RegisterAsync(client, role);
        var auth = await LoginAsync(client, account);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return account;
    }

    /// <summary>Self-registration can never create an Admin (by design), so
    /// admin accounts for tests are seeded straight into the database.</summary>
    public static async Task<TestAccount> SeedAndAuthenticateAdminAsync(
        IServiceProvider rootServices, HttpClient client)
    {
        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var suffix = Guid.NewGuid().ToString("N")[..10];
        var account = new TestAccount($"admin{suffix}@test.local", $"admin{suffix}", "Passw0rd1");

        db.Users.Add(new User
        {
            UserName = account.UserName,
            FirstName = "Admin",
            LastName = "User",
            Email = account.Email,
            PasswordHash = hasher.Hash(account.Password),
            RoleId = (int)UserRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var auth = await LoginAsync(client, account);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return account;
    }
}
