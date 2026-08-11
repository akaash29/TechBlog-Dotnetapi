namespace UserRegistration.Application.DTOs.Auth;

public sealed class LoginRequest
{
    /// <summary>Accepts either the account's email address or its username.</summary>
    public string EmailOrUserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
