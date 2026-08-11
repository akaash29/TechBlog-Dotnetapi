using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;

    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByEmailOrUserNameAsync(string emailOrUserName, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(
            u => u.Email == emailOrUserName || u.UserName == emailOrUserName,
            cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Users.AsNoTracking().Include(u => u.Role).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        UserRole? role,
        bool? isActive,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (role is not null)
        {
            query = query.Where(u => u.RoleId == (int)role.Value);
        }

        if (isActive is not null)
        {
            query = query.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // EF Core translates Contains to a LIKE '%...%' — SQL Server's
            // default collation is already case-insensitive, so this
            // matches the "search by name or handle" box's expectation
            // without needing an explicit ToLower() on both sides.
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.UserName.Contains(search) ||
                u.Email.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(u => u.UserName == userName, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _dbContext.Users.AddAsync(user, cancellationToken);

    public void Update(User user) => _dbContext.Users.Update(user);

    public void Remove(User user) => _dbContext.Users.Remove(user);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Defense-in-depth against races: two concurrent requests can both pass the
            // application-level exists-check before either commits. The unique indexes on
            // Users.Email and Users.UserName are the actual source of truth, so a violation
            // reported by EF Core here is translated into a domain-friendly conflict.
            throw new ConflictException("A user with the same email or username already exists.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlException &&
        sqlException.Errors.Cast<SqlError>().Any(error =>
            error.Number is SqlUniqueConstraintViolation or SqlUniqueIndexViolation);
}
