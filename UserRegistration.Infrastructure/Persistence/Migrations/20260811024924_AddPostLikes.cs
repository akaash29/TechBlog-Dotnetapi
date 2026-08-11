using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserRegistration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlogPostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostLikes_BlogPosts_BlogPostId",
                        column: x => x.BlogPostId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_BlogPostId_UserId",
                table: "PostLikes",
                columns: new[] { "BlogPostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_UserId",
                table: "PostLikes",
                column: "UserId");

            // Extends sp_GetTopBlogPosts (see AddViewCountAndBlogPostIndexes) with
            // two optional exclusion filters — "suggested for you" reuses this same
            // proc/repository method rather than duplicating a near-identical one.
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the pre-exclusion-filters shape of the proc (see
            // AddViewCountAndBlogPostIndexes) so a rollback leaves the database
            // exactly as that earlier migration left it.
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

            migrationBuilder.DropTable(
                name: "PostLikes");
        }
    }
}
