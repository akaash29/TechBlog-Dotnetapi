namespace UserRegistration.Application.Common.Interfaces;

/// <summary>Reads the signed-in user's identity out of the current HTTP request's claims.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>Whether the signed-in user has the given role (e.g. "Admin").
    /// False when no one is signed in.</summary>
    bool IsInRole(string role);
}
