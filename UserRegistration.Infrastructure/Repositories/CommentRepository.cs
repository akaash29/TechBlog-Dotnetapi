using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Domain.Entities;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CommentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Comments
            .Include(c => c.CreatedByUser)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Comment>> GetByBlogPostIdAsync(int blogPostId, CancellationToken cancellationToken = default) =>
        await _dbContext.Comments
            .AsNoTracking()
            .Include(c => c.CreatedByUser)
            .Where(c => c.BlogPostId == blogPostId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default) =>
        await _dbContext.Comments.AddAsync(comment, cancellationToken);

    public void Update(Comment comment) => _dbContext.Comments.Update(comment);

    public void Remove(Comment comment) => _dbContext.Comments.Remove(comment);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
