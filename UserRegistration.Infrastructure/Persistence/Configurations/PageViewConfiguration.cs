using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Infrastructure.Persistence.Configurations;

public sealed class PageViewConfiguration : IEntityTypeConfiguration<PageView>
{
    public void Configure(EntityTypeBuilder<PageView> builder)
    {
        builder.ToTable("PageViews");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.Country)
            .HasMaxLength(100);

        builder.Property(v => v.IpAddress)
            .HasMaxLength(45); // long enough for an IPv6 address

        builder.Property(v => v.UserAgent)
            .HasMaxLength(500);

        builder.Property(v => v.DeviceType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.VisitedAt)
            .IsRequired();

        builder.Property(v => v.LastActivityAt)
            .IsRequired();

        // A page view can outlive its user (deleted account) — keep the
        // historical record, just drop the link.
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.VisitedAt);
        builder.HasIndex(v => v.SessionId);
        builder.HasIndex(v => v.LastActivityAt);
    }
}
