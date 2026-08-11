using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    void Update(RefreshToken refreshToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
