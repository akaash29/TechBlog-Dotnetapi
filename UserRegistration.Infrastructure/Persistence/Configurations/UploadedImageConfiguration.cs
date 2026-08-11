using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Infrastructure.Persistence.Configurations;

public sealed class UploadedImageConfiguration : IEntityTypeConfiguration<UploadedImage>
{
    public void Configure(EntityTypeBuilder<UploadedImage> builder)
    {
        builder.ToTable("UploadedImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImagePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.CreatedDate)
            .IsRequired();

        // Nullable — an image is uploaded before the post it belongs to is
        // ever saved (see the note on the entity), so it starts unlinked.
        builder.HasOne(i => i.BlogPost)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.UpdatedByUser)
            .WithMany()
            .HasForeignKey(i => i.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.BlogPostId);

        builder.HasIndex(i => i.CreatedBy);
    }
}
