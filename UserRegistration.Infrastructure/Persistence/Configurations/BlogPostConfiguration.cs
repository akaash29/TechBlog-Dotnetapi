using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Infrastructure.Persistence.Configurations;

public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Header)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(500);

        // The full post body, authored as HTML — effectively unbounded. No
        // explicit HasColumnType: an unbounded string property already maps
        // to nvarchar(max) on SQL Server by convention, and a hardcoded
        // SQL-Server-specific type string here would break other providers
        // (e.g. SQLite in integration tests).
        builder.Property(p => p.PostHtml)
            .IsRequired();

        // A blob URL, not the file itself.
        builder.Property(p => p.CoverImagePath)
            .HasMaxLength(1000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.BlogPostStatus.Draft)
            .HasConversion<int>();

        builder.Property(p => p.LikesCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.CommentsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedDate)
            .IsRequired();

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict (not Cascade) on both — deleting a user shouldn't cascade-delete
        // their posts, and SQL Server would reject two cascade paths into Users anyway.
        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UpdatedByUser)
            .WithMany()
            .HasForeignKey(p => p.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CategoryId);

        builder.HasIndex(p => p.CreatedDate);

        // Back the "most viewed"/"most liked" rails' ORDER BY ... DESC queries.
        builder.HasIndex(p => p.ViewCount);

        builder.HasIndex(p => p.LikesCount);

        // Every listing query (feed/journal/top) filters to Published, and
        // the pending-approval page filters to PendingApproval.
        builder.HasIndex(p => p.Status);
    }
}
