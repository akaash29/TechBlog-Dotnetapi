using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Common.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>All comments for a post, oldest first (reading order),
    /// with the commenter's identity loaded.</summary>
    Task<IReadOnlyList<Comment>> GetByBlogPostIdAsync(int blogPostId, CancellationToken cancellationToken = default);

    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);

    void Update(Comment comment);

    void Remove(Comment comment);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
