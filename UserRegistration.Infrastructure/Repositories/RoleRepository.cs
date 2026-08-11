using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(cancellationToken);
}
