using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UserRegistration.IntegrationTests.Infrastructure;

namespace UserRegistration.IntegrationTests.Controllers;

public sealed class UsersEndpointsTests : IntegrationTestBase
{
    public UsersEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    // ---------- register ----------

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var response = await Client.PostAsJsonAsync("/api/users/register", new
        {
            UserName = $"user{suffix}",
            FirstName = "Test",
            LastName = "User",
            Email = $"user{suffix}@test.local",
            Role = "Writer",
            Password = "Passw0rd1"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Writer", body.GetProperty("role").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var account = await TestAuth.RegisterAsync(Client);

        var response = await Client.PostAsJsonAsync("/api/users/register", new
        {
            UserName = $"another{Guid.NewGuid():N}"[..15],
            FirstName = "Test",
            LastName = "User",
            Email = account.Email,
            Role = "Writer",
            Password = "Passw0rd1"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAdminRole_ReturnsBadRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var response = await Client.PostAsJsonAsync("/api/users/register", new
        {
            UserName = $"user{suffix}",
            FirstName = "Test",
            LastName = "User",
            Email = $"user{suffix}@test.local",
            Role = "Admin",
            Password = "Passw0rd1"
        });

        // Self-registration must never grant Admin — validated, not just ignored.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- admin create ----------

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreatedUser()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var response = await Client.PostAsJsonAsync("/api/users", new
        {
            UserName = $"created{suffix}",
            FirstName = "Created",
            LastName = "ByAdmin",
            Email = $"created{suffix}@test.local",
            Role = "Editor",
            Password = "Passw0rd1"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var response = await Client.PostAsJsonAsync("/api/users", new
        {
            UserName = $"created{suffix}",
            FirstName = "Created",
            LastName = "ByWriter",
            Email = $"created{suffix}@test.local",
            Role = "Editor",
            Password = "Passw0rd1"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new
        {
            UserName = "whoever",
            FirstName = "No",
            LastName = "Auth",
            Email = "whoever@test.local",
            Role = "Writer",
            Password = "Passw0rd1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- get by id / by email / paged ----------

    [Fact]
    public async Task GetById_ForExistingUser_ReturnsUser()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.GetAsync($"/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownUser_ReturnsNotFound()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByEmail_ForExistingUser_ReturnsUser()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.GetAsync($"/api/users/by-email?email={Uri.EscapeDataString(account.Email)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsOk()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);

        var response = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithDefaultParameters_ReturnsOk()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.GetAsync("/api/users/paged");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- update / password / status / delete ----------

    [Fact]
    public async Task Update_OwnProfile_ReturnsOk()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{id}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Role = "Writer",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForUnknownUser_ReturnsNotFound()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Role = "Writer",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithPhoneAndCity_PersistsBothFields()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{id}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Phone = "+91 98490 22118",
            City = "Hyderabad",
            Role = "Writer",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("+91 98490 22118", body.GetProperty("phone").GetString());
        Assert.Equal("Hyderabad", body.GetProperty("city").GetString());
    }

    [Fact]
    public async Task Update_WithInvalidPhone_ReturnsBadRequest()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{id}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Phone = "not-a-phone-number!!",
            Role = "Writer",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherUserAsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var anon = Factory.CreateClient();
        var other = await TestAuth.RegisterAsync(anon);
        var otherId = await GetUserIdByEmailAsync(other.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{otherId}", new
        {
            FirstName = "Hijacked",
            LastName = "Name",
            Role = "Writer",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_AttemptingRoleEscalationAsNonAdmin_KeepsOriginalRole()
    {
        // A Writer's own profile update sends Role = "Admin" — the API must not
        // trust that and self-promote the caller.
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client, role: "Writer");
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{id}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Role = "Admin",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Writer", body.GetProperty("role").GetString());
    }

    // ---------- profile image ----------

    [Fact]
    public async Task UploadProfileImage_AsOwner_ReturnsUserWithImagePath()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        using var content = BuildMultipartContent(new byte[1024], "image/jpeg", "avatar.jpg");
        var response = await Client.PostAsync($"/api/users/{id}/profile-image", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var imagePath = body.GetProperty("profileImagePath").GetString();
        Assert.False(string.IsNullOrWhiteSpace(imagePath));
        // grouped under the "userimage" container in the caller's own virtual folder
        Assert.Contains("userimage", imagePath);
        Assert.Contains(id.ToString("N"), imagePath);
    }

    [Fact]
    public async Task UploadProfileImage_ForAnotherUserAsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        using var anon = Factory.CreateClient();
        var other = await TestAuth.RegisterAsync(anon);
        var otherId = await GetUserIdByEmailAsync(other.Email);

        using var content = BuildMultipartContent(new byte[1024], "image/jpeg", "avatar.jpg");
        var response = await Client.PostAsync($"/api/users/{otherId}/profile-image", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfileImage_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var content = BuildMultipartContent(new byte[1024], "image/jpeg", "avatar.jpg");

        var response = await Client.PostAsync($"/api/users/{Guid.NewGuid()}/profile-image", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfileImage_WithDisallowedContentType_ReturnsBadRequest()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        using var content = BuildMultipartContent(new byte[1024], "text/plain", "notes.txt");
        var response = await Client.PostAsync($"/api/users/{id}/profile-image", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent BuildMultipartContent(byte[] bytes, string contentType, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_ReturnsNoContent()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{id}/password", new
        {
            CurrentPassword = account.Password,
            NewPassword = "NewPassw0rd1",
            ConfirmNewPassword = "NewPassw0rd1"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsUnauthorized()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PutAsJsonAsync($"/api/users/{id}/password", new
        {
            CurrentPassword = "TheWrongPassword1",
            NewPassword = "NewPassw0rd1",
            ConfirmNewPassword = "NewPassw0rd1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetStatus_AsAdmin_ReturnsOk()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);

        // deactivate a fresh throwaway user via a second, anonymous client
        using var anon = Factory.CreateClient();
        var target = await TestAuth.RegisterAsync(anon);
        var targetId = await GetUserIdByEmailAsync(target.Email);

        var response = await Client.PatchAsJsonAsync($"/api/users/{targetId}/status", new { IsActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetStatus_AsNonAdmin_ReturnsForbidden()
    {
        var account = await TestAuth.RegisterAndAuthenticateAsync(Client);
        var id = await GetUserIdByEmailAsync(account.Email);

        var response = await Client.PatchAsJsonAsync($"/api/users/{id}/status", new { IsActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        await TestAuth.SeedAndAuthenticateAdminAsync(Factory.Services, Client);

        using var anon = Factory.CreateClient();
        var target = await TestAuth.RegisterAsync(anon);
        var targetId = await GetUserIdByEmailAsync(target.Email);

        var response = await Client.DeleteAsync($"/api/users/{targetId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsNonAdmin_ReturnsForbidden()
    {
        await TestAuth.RegisterAndAuthenticateAsync(Client);

        var response = await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        var response = await Client.GetAsync($"/api/users/by-email?email={Uri.EscapeDataString(email)}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
