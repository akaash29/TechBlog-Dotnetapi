using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasData(
            new Category { Id = 1, Name = "Technology News" },
            new Category { Id = 2, Name = "AI & Machine Learning" },
            new Category { Id = 3, Name = "Programming & Development" },
            new Category { Id = 4, Name = "Cybersecurity" },
            new Category { Id = 5, Name = "Cloud & Infrastructure" });
    }
}
