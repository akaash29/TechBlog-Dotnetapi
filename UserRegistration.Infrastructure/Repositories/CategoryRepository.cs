using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
