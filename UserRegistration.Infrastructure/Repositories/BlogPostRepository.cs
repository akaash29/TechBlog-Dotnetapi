using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.BlogPosts;
using UserRegistration.Domain.Entities;
using UserRegistration.Domain.Enums;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

/// <summary>
/// The feed/journal/top-posts reads go through stored procedures
/// (sp_GetBlogPostsPaged, sp_GetTopBlogPosts — see the
/// AddViewCountAndFeedStoredProcedures migration) when running against SQL
/// Server, for exactly the pages that read the most and benefit most from a
/// plan SQL Server can cache and reuse. Everywhere else (SQLite in
/// integration tests — stored procedures are a SQL Server-only concept) this
/// falls back to an equivalent LINQ query, so behaviour is identical either
/// way; only the plumbing differs.
/// </summary>
public sealed class BlogPostRepository : IBlogPostRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BlogPostRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BlogPost blogPost, CancellationToken cancellationToken = default) =>
        await _dbContext.BlogPosts.AddAsync(blogPost, cancellationToken);

    public Task<BlogPost?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.BlogPosts
            .Include(p => p.Category)
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<(IReadOnlyList<BlogPostSummaryDto> Items, int TotalCount)> GetFeedPagedAsync(
        string tab, int? preferredCategoryId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        _dbContext.Database.IsSqlServer()
            ? GetPagedViaStoredProcedureAsync(tab, categoryId: null, preferredCategoryId, page, pageSize, cancellationToken)
            : GetPagedViaLinqAsync(tab, categoryId: null, preferredCategoryId, page, pageSize, cancellationToken);

    public Task<(IReadOnlyList<BlogPostSummaryDto> Items, int TotalCount)> GetJournalPagedAsync(
        int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        _dbContext.Database.IsSqlServer()
            ? GetPagedViaStoredProcedureAsync("latest", categoryId, preferredCategoryId: null, page, pageSize, cancellationToken)
            : GetPagedViaLinqAsync("latest", categoryId, preferredCategoryId: null, page, pageSize, cancellationToken);

    public Task<IReadOnlyList<BlogPostSummaryDto>> GetTopAsync(
        string metric, int take, Guid? excludeUserId = null, int? excludeCategoryId = null, CancellationToken cancellationToken = default) =>
        _dbContext.Database.IsSqlServer()
            ? GetTopViaStoredProcedureAsync(metric, take, excludeUserId, excludeCategoryId, cancellationToken)
            : GetTopViaLinqAsync(metric, take, excludeUserId, excludeCategoryId, cancellationToken);

    public async Task<int?> GetPreferredCategoryIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.BlogPosts
            .Where(p => p.CreatedBy == userId)
            .GroupBy(p => p.CategoryId)
            .OrderByDescending(g => g.Count())
            .Select(g => (int?)g.Key)
            .FirstOrDefaultAsync(cancellationToken);

    public Task IncrementViewCountAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.BlogPosts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), cancellationToken);

    public async Task<(bool Liked, int LikesCount)> ToggleLikeAsync(
        int blogPostId, Guid userId, CancellationToken cancellationToken = default)
    {
        // ExecuteUpdateAsync below is a bulk operation — it runs immediately
        // against the database rather than joining the change tracker's batch,
        // so it does NOT share atomicity with the PostLikes add/remove (which
        // only takes effect on SaveChangesAsync) unless both are wrapped in one
        // transaction here. Without it, a failure between the two calls could
        // leave LikesCount out of sync with the actual PostLikes rows.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existing = await _dbContext.PostLikes
            .FirstOrDefaultAsync(l => l.BlogPostId == blogPostId && l.UserId == userId, cancellationToken);

        bool liked;
        if (existing is not null)
        {
            _dbContext.PostLikes.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.BlogPosts
                .Where(p => p.Id == blogPostId && p.LikesCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikesCount, p => p.LikesCount - 1), cancellationToken);
            liked = false;
        }
        else
        {
            await _dbContext.PostLikes.AddAsync(
                new PostLike { BlogPostId = blogPostId, UserId = userId, CreatedDate = DateTime.UtcNow },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.BlogPosts
                .Where(p => p.Id == blogPostId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikesCount, p => p.LikesCount + 1), cancellationToken);
            liked = true;
        }

        var likesCount = await _dbContext.BlogPosts
            .Where(p => p.Id == blogPostId)
            .Select(p => p.LikesCount)
            .FirstAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (liked, likesCount);
    }

    public async Task<IReadOnlyList<int>> GetLikedPostIdsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.PostLikes
            .Where(l => l.UserId == userId)
            .Select(l => l.BlogPostId)
            .ToListAsync(cancellationToken);

    public void Update(BlogPost blogPost) => _dbContext.BlogPosts.Update(blogPost);

    public void Remove(BlogPost blogPost) => _dbContext.BlogPosts.Remove(blogPost);

    public async Task<IReadOnlyList<BlogPostSummaryDto>> GetPendingApprovalAsync(CancellationToken cancellationToken = default) =>
        // Low-traffic, admin-only page — a plain LINQ query (no stored
        // procedure) is plenty here.
        await _dbContext.BlogPosts
            .AsNoTracking()
            .Where(p => p.Status == BlogPostStatus.PendingApproval)
            .OrderBy(p => p.CreatedDate)
            .Select(SummaryProjection)
            .ToListAsync(cancellationToken);

    public async Task<AuthorStatsDto> GetAuthorStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // A single aggregate query — Count/Sum all translate to SQL fine, no
        // GroupBy involved.
        var totals = await _dbContext.BlogPosts
            .AsNoTracking()
            .Where(p => p.CreatedBy == userId && p.Status == BlogPostStatus.Published)
            .GroupBy(p => 1)
            .Select(g => new
            {
                Posts = g.Count(),
                Reads = g.Sum(p => p.ViewCount),
                Comments = g.Sum(p => p.CommentsCount),
                Likes = g.Sum(p => p.LikesCount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new AuthorStatsDto
        {
            Posts = totals?.Posts ?? 0,
            Reads = totals?.Reads ?? 0,
            Comments = totals?.Comments ?? 0,
            Likes = totals?.Likes ?? 0,
        };
    }

    public async Task<IReadOnlyList<int>> GetPublishedPostIdsByAuthorAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.BlogPosts
            .AsNoTracking()
            .Where(p => p.CreatedBy == userId && p.Status == BlogPostStatus.Published)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    // ---------------------------------------------------------------
    // LINQ fallback — mirrors sp_GetBlogPostsPaged's semantics exactly.
    // ---------------------------------------------------------------

    private async Task<(IReadOnlyList<BlogPostSummaryDto>, int)> GetPagedViaLinqAsync(
        string tab, int? categoryId, int? preferredCategoryId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.BlogPosts.AsNoTracking().Where(p => p.Status == BlogPostStatus.Published);
        if (categoryId is int cid)
        {
            query = query.Where(p => p.CategoryId == cid);
        }

        query = tab switch
        {
            "trending" => query
                .OrderByDescending(p => p.ViewCount + p.LikesCount * 6 + p.CommentsCount * 3)
                .ThenByDescending(p => p.CreatedDate),
            "foryou" => query
                .OrderByDescending(p => preferredCategoryId != null && p.CategoryId == preferredCategoryId)
                .ThenByDescending(p => p.ViewCount * 0.4 + p.LikesCount * 4)
                .ThenByDescending(p => p.CreatedDate),
            _ => query.OrderByDescending(p => p.CreatedDate),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SummaryProjection)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private async Task<IReadOnlyList<BlogPostSummaryDto>> GetTopViaLinqAsync(
        string metric, int take, Guid? excludeUserId, int? excludeCategoryId, CancellationToken cancellationToken)
    {
        var query = _dbContext.BlogPosts.AsNoTracking().Where(p => p.Status == BlogPostStatus.Published);
        if (excludeUserId is { } excludeUser)
        {
            query = query.Where(p => p.CreatedBy != excludeUser);
        }
        if (excludeCategoryId is { } excludeCategory)
        {
            query = query.Where(p => p.CategoryId != excludeCategory);
        }

        query = metric switch
        {
            "likes" => query.OrderByDescending(p => p.LikesCount).ThenByDescending(p => p.CreatedDate),
            "comments" => query.OrderByDescending(p => p.CommentsCount).ThenByDescending(p => p.CreatedDate),
            _ => query.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.CreatedDate),
        };

        return await query.Take(take).Select(SummaryProjection).ToListAsync(cancellationToken);
    }

    // A genuine expression tree (not a helper method call) — EF Core needs to
    // see the actual member accesses to translate this into a SQL JOIN.
    // Wrapping this in a regular method call (e.g. `.Select(p => ToDto(p))`)
    // would silently defeat that: EF can't look inside an opaque method, so
    // it falls back to loading bare BlogPost rows and invoking the method
    // client-side — with Category/CreatedByUser never populated (no
    // Include, and the entities are AsNoTracking projections), every
    // navigation-property access below would throw NullReferenceException.
    private static readonly Expression<Func<BlogPost, BlogPostSummaryDto>> SummaryProjection = p => new BlogPostSummaryDto
    {
        Id = p.Id,
        Header = p.Header,
        Title = p.Title,
        Description = p.Description,
        CoverImagePath = p.CoverImagePath,
        // Enum.ToString() has no SQL equivalent and EF Core can't translate
        // it — a nested ternary of plain comparisons/literals, on the other
        // hand, translates cleanly to a SQL CASE expression.
        Status = p.Status == BlogPostStatus.Draft ? "Draft"
            : p.Status == BlogPostStatus.PendingApproval ? "PendingApproval"
            : "Published",
        LikesCount = p.LikesCount,
        CommentsCount = p.CommentsCount,
        ViewCount = p.ViewCount,
        CategoryId = p.CategoryId,
        CategoryName = p.Category.Name,
        CreatedBy = p.CreatedBy,
        AuthorName = p.CreatedByUser.FirstName + " " + p.CreatedByUser.LastName,
        AuthorProfileImagePath = p.CreatedByUser.ProfileImagePath,
        CreatedDate = p.CreatedDate,
    };

    // ---------------------------------------------------------------
    // Stored-procedure path (SQL Server only).
    // ---------------------------------------------------------------

    private async Task<(IReadOnlyList<BlogPostSummaryDto>, int)> GetPagedViaStoredProcedureAsync(
        string tab, int? categoryId, int? preferredCategoryId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_GetBlogPostsPaged";
            command.CommandType = CommandType.StoredProcedure;
            if (_dbContext.Database.CurrentTransaction is { } transaction)
            {
                command.Transaction = (SqlTransaction)transaction.GetDbTransaction();
            }

            command.Parameters.Add(new SqlParameter("@Tab", tab));
            command.Parameters.Add(new SqlParameter("@CategoryId", (object?)categoryId ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@PreferredCategoryId", (object?)preferredCategoryId ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Page", page));
            command.Parameters.Add(new SqlParameter("@PageSize", pageSize));

            var items = new List<BlogPostSummaryDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapSummary(reader));
            }

            var totalCount = 0;
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            }

            return (items, totalCount);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<IReadOnlyList<BlogPostSummaryDto>> GetTopViaStoredProcedureAsync(
        string metric, int take, Guid? excludeUserId, int? excludeCategoryId, CancellationToken cancellationToken)
    {
        var connection = (SqlConnection)_dbContext.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_GetTopBlogPosts";
            command.CommandType = CommandType.StoredProcedure;
            if (_dbContext.Database.CurrentTransaction is { } transaction)
            {
                command.Transaction = (SqlTransaction)transaction.GetDbTransaction();
            }

            command.Parameters.Add(new SqlParameter("@Metric", metric));
            command.Parameters.Add(new SqlParameter("@Top", take));
            command.Parameters.Add(new SqlParameter("@ExcludeUserId", (object?)excludeUserId ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@ExcludeCategoryId", (object?)excludeCategoryId ?? DBNull.Value));

            var items = new List<BlogPostSummaryDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapSummary(reader));
            }

            return items;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static BlogPostSummaryDto MapSummary(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Header = reader.GetString(reader.GetOrdinal("Header")),
        Title = reader.GetString(reader.GetOrdinal("Title")),
        Description = reader.GetString(reader.GetOrdinal("Description")),
        CoverImagePath = reader.IsDBNull(reader.GetOrdinal("CoverImagePath")) ? null : reader.GetString(reader.GetOrdinal("CoverImagePath")),
        Status = ((BlogPostStatus)reader.GetInt32(reader.GetOrdinal("Status"))).ToString(),
        LikesCount = reader.GetInt32(reader.GetOrdinal("LikesCount")),
        CommentsCount = reader.GetInt32(reader.GetOrdinal("CommentsCount")),
        ViewCount = reader.GetInt32(reader.GetOrdinal("ViewCount")),
        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
        CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
        CreatedBy = reader.GetGuid(reader.GetOrdinal("CreatedBy")),
        AuthorName = reader.GetString(reader.GetOrdinal("AuthorName")),
        AuthorProfileImagePath = reader.IsDBNull(reader.GetOrdinal("AuthorProfileImagePath")) ? null : reader.GetString(reader.GetOrdinal("AuthorProfileImagePath")),
        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
    };
}
