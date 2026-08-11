using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRegistration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewCountAndBlogPostIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "BlogPosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_LikesCount",
                table: "BlogPosts",
                column: "LikesCount");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_ViewCount",
                table: "BlogPosts",
                column: "ViewCount");

            // Backs the feed and journal pages' post listings — see
            // BlogPostRepository, which calls these when running against SQL
            // Server (falling back to an equivalent LINQ query everywhere
            // else, e.g. SQLite in integration tests, where stored
            // procedures aren't a thing).
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

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_GetTopBlogPosts
                    @Metric NVARCHAR(20) = 'views',
                    @Top INT = 4
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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetBlogPostsPaged;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetTopBlogPosts;");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_LikesCount",
                table: "BlogPosts");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_ViewCount",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "BlogPosts");
        }
    }
}
