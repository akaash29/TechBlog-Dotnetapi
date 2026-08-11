using UserRegistration.Domain.Common;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Domain.Entities;

public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public string? Phone { get; set; }

    public string? City { get; set; }

    /// <summary>The profile photo's public blob URL, or null until the user uploads one.</summary>
    public string? ProfileImagePath { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public void UpdateProfile(string firstName, string lastName, string? phone, string? city, UserRole role, bool isActive)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        City = city;
        RoleId = (int)role;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProfileImage(string profileImagePath)
    {
        ProfileImagePath = profileImagePath;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
