using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Text)
            .HasMaxLength(4000);

        builder.Property(m => m.AttachmentUrl)
            .HasMaxLength(1000);

        builder.Property(m => m.AttachmentFileName)
            .HasMaxLength(260);

        builder.Property(m => m.AttachmentContentType)
            .HasMaxLength(200);

        builder.Property(m => m.VoiceNoteUrl)
            .HasMaxLength(1000);

        builder.Property(m => m.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedDate)
            .IsRequired();

        // Restrict on both — deleting a user shouldn't cascade-delete every
        // conversation they were part of, and SQL Server would reject two
        // cascade paths into Users from the same table anyway.
        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Recipient)
            .WithMany()
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backs "messages between these two people, newest first" (a thread)
        // and "my conversations, grouped by the other participant".
        builder.HasIndex(m => new { m.SenderId, m.RecipientId, m.CreatedDate });
        builder.HasIndex(m => new { m.RecipientId, m.SenderId, m.CreatedDate });

        // Backs the unread-count badge and "mark thread as read".
        builder.HasIndex(m => new { m.RecipientId, m.IsRead });
    }
}
