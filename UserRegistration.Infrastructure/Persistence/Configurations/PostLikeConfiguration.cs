using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Infrastructure.Persistence.Configurations;

public sealed class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("PostLikes");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.CreatedDate)
            .IsRequired();

        // One like per (post, user) — this is what makes toggling safe under a
        // race (two rapid double-clicks can't both insert) and is the query the
        // "have I liked this" and "toggle" paths both lean on.
        builder.HasIndex(l => new { l.BlogPostId, l.UserId })
            .IsUnique();

        builder.HasOne(l => l.BlogPost)
            .WithMany()
            .HasForeignKey(l => l.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
