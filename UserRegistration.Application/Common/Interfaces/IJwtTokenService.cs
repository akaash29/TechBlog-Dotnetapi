using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Creates a short-lived signed JWT access token carrying the user's identity and role claims.</summary>
    string GenerateAccessToken(User user);

    /// <summary>Creates a cryptographically random, opaque refresh token value.</summary>
    string GenerateRefreshToken();

    /// <summary>Hashes a refresh token value for storage/lookup, so the raw token is never persisted.</summary>
    string HashRefreshToken(string refreshToken);
}
