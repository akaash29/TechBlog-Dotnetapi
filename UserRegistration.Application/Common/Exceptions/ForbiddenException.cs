namespace UserRegistration.Application.Common.Exceptions;

/// <summary>The caller is authenticated but isn't allowed to perform this
/// specific action (e.g. editing another user's profile) — distinct from
/// UnauthorizedException, which means "not signed in" or "bad credentials".</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
