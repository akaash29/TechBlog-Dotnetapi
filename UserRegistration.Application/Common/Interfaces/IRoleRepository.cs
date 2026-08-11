using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
