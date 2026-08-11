using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRegistration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsDraftWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Status replaces the old boolean IsDraft with a 3-state editorial
            // workflow (see BlogPostStatus): Draft = 0, PendingApproval = 1,
            // Published = 2. Added before IsDraft is dropped so the backfill
            // below can read the old values.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BlogPosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Every existing row was either a draft or already published —
            // PendingApproval is a new state nothing in the old data can map
            // to, so IsDraft = 0 becomes Published outright.
            migrationBuilder.Sql(
                """
                UPDATE dbo.BlogPosts SET Status = CASE WHEN IsDraft = 1 THEN 0 ELSE 2 END;
                """);

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "BlogPosts");

            // Every listing query (feed/journal/top) filters to Published, and
            // the pending-approval page filters to PendingApproval.
            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Status",
                table: "BlogPosts",
                column: "Status");

            // Both procs move from "WHERE IsDraft = 0" to "WHERE Status = 2"
            // (Published) and now also return Status so callers can tell
            // Draft/PendingApproval/Published apart (see BlogPostRepository.MapSummary).
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_GetBlogPostsPaged
                    @Tab NVARCHAR(20) = 'latest',
                    @CategoryId INT = NULL,
                    @PreferredCategoryId INT = NULL,
                    @Page INT = 1,
                    @PageSize INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 10;

                    SELECT
                        bp.Id, bp.Header, bp.Title, bp.Description, bp.CoverImagePath,
                        bp.Status, bp.LikesCount, bp.CommentsCount, bp.ViewCount,
                        bp.CategoryId, c.Name AS CategoryName,
                        bp.CreatedBy,
                        u.FirstName + ' ' + u.LastName AS AuthorName,
                        u.ProfileImagePath AS AuthorProfileImagePath,
                        bp.CreatedDate
                    FROM dbo.BlogPosts bp
                    INNER JOIN dbo.Categories c ON c.Id = bp.CategoryId
                    INNER JOIN dbo.Users u ON u.Id = bp.CreatedBy
                    WHERE bp.Status = 2
                      AND (@CategoryId IS NULL OR bp.CategoryId = @CategoryId)
                    ORDER BY
                        CASE WHEN @Tab = 'trending'
                             THEN bp.ViewCount + bp.LikesCount * 6 + bp.CommentsCount * 3 END DESC,
                        CASE WHEN @Tab = 'foryou' AND @PreferredCategoryId IS NOT NULL
                                  AND bp.CategoryId = @PreferredCategoryId
                             THEN 1 ELSE 0 END DESC,
                        CASE WHEN @Tab = 'foryou'
                             THEN bp.ViewCount * 0.4 + bp.LikesCount * 4 END DESC,
                        bp.CreatedDate DESC
                    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(*) AS TotalCount
                    FROM dbo.BlogPosts bp
                    WHERE bp.Status = 2
                      AND (@CategoryId IS NULL OR bp.CategoryId = @CategoryId);
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_GetTopBlogPosts
                    @Metric NVARCHAR(20) = 'views',
                    @Top INT = 4,
                    @ExcludeUserId UNIQUEIDENTIFIER = NULL,
                    @ExcludeCategoryId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Top < 1 SET @Top = 4;

                    SELECT TOP (@Top)
                        bp.Id, bp.Header, bp.Title, bp.Description, bp.CoverImagePath,
                        bp.Status, bp.LikesCount, bp.CommentsCount, bp.ViewCount,
                        bp.CategoryId, c.Name AS CategoryName,
                        bp.CreatedBy,
                        u.FirstName + ' ' + u.LastName AS AuthorName,
                        u.ProfileImagePath AS AuthorProfileImagePath,
                        bp.CreatedDate
                    FROM dbo.BlogPosts bp
                    INNER JOIN dbo.Categories c ON c.Id = bp.CategoryId
                    INNER JOIN dbo.Users u ON u.Id = bp.CreatedBy
                    WHERE bp.Status = 2
                      AND (@ExcludeUserId IS NULL OR bp.CreatedBy <> @ExcludeUserId)
                      AND (@ExcludeCategoryId IS NULL OR bp.CategoryId <> @ExcludeCategoryId)
                    ORDER BY
                        CASE WHEN @Metric = 'likes' THEN bp.LikesCount
                             WHEN @Metric = 'comments' THEN bp.CommentsCount
                             ELSE bp.ViewCount END DESC,
                        bp.CreatedDate DESC;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the pre-Status shape of both procs (see AddPostLikes /
            // AddViewCountAndBlogPostIndexes) so a rollback leaves the
            // database exactly as those earlier migrations left it.
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_GetTopBlogPosts
                    @Metric NVARCHAR(20) = 'views',
                    @Top INT = 4,
                    @ExcludeUserId UNIQUEIDENTIFIER = NULL,
                    @ExcludeCategoryId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Top < 1 SET @Top = 4;

                    SELECT TOP (@Top)
                        bp.Id, bp.Header, bp.Title, bp.Description, bp.CoverImagePath,
                        bp.LikesCount, bp.CommentsCount, bp.ViewCount,
                        bp.CategoryId, c.Name AS CategoryName,
                        bp.CreatedBy,
                        u.FirstName + ' ' + u.LastName AS AuthorName,
                        u.ProfileImagePath AS AuthorProfileImagePath,
                        bp.CreatedDate
                    FROM dbo.BlogPosts bp
                    INNER JOIN dbo.Categories c ON c.Id = bp.CategoryId
                    INNER JOIN dbo.Users u ON u.Id = bp.CreatedBy
                    WHERE bp.IsDraft = 0
                      AND (@ExcludeUserId IS NULL OR bp.CreatedBy <> @ExcludeUserId)
                      AND (@ExcludeCategoryId IS NULL OR bp.CategoryId <> @ExcludeCategoryId)
                    ORDER BY
                        CASE WHEN @Metric = 'likes' THEN bp.LikesCount
                             WHEN @Metric = 'comments' THEN bp.CommentsCount
                             ELSE bp.ViewCount END DESC,
                        bp.CreatedDate DESC;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_GetBlogPostsPaged
                    @Tab NVARCHAR(20) = 'latest',
                    @CategoryId INT = NULL,
                    @PreferredCategoryId INT = NULL,
                    @Page INT = 1,
                    @PageSize INT = 10
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 10;

                    SELECT
                        bp.Id, bp.Header, bp.Title, bp.Description, bp.CoverImagePath,
                        bp.LikesCount, bp.CommentsCount, bp.ViewCount,
                        bp.CategoryId, c.Name AS CategoryName,
                        bp.CreatedBy,
                        u.FirstName + ' ' + u.LastName AS AuthorName,
                        u.ProfileImagePath AS AuthorProfileImagePath,
                        bp.CreatedDate
                    FROM dbo.BlogPosts bp
                    INNER JOIN dbo.Categories c ON c.Id = bp.CategoryId
                    INNER JOIN dbo.Users u ON u.Id = bp.CreatedBy
                    WHERE bp.IsDraft = 0
                      AND (@CategoryId IS NULL OR bp.CategoryId = @CategoryId)
                    ORDER BY
                        CASE WHEN @Tab = 'trending'
                             THEN bp.ViewCount + bp.LikesCount * 6 + bp.CommentsCount * 3 END DESC,
                        CASE WHEN @Tab = 'foryou' AND @PreferredCategoryId IS NOT NULL
                                  AND bp.CategoryId = @PreferredCategoryId
                             THEN 1 ELSE 0 END DESC,
                        CASE WHEN @Tab = 'foryou'
                             THEN bp.ViewCount * 0.4 + bp.LikesCount * 4 END DESC,
                        bp.CreatedDate DESC
                    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(*) AS TotalCount
                    FROM dbo.BlogPosts bp
                    WHERE bp.IsDraft = 0
                      AND (@CategoryId IS NULL OR bp.CategoryId = @CategoryId);
                END
                """);

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_Status",
                table: "BlogPosts");

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "BlogPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE dbo.BlogPosts SET IsDraft = CASE WHEN Status = 0 THEN 1 ELSE 0 END;
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BlogPosts");
        }
    }
}
