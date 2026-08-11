namespace UserRegistration.Application.DTOs.Users;

public sealed class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? City { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
