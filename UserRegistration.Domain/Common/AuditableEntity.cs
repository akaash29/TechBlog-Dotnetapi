namespace UserRegistration.Domain.Common;

/// <summary>
/// Base for content entities with an auto-incrementing int key plus
/// created-by/updated-by audit tracking against <see cref="Entities.User"/>.
/// Distinct from <see cref="BaseEntity"/> (Guid key, no per-user audit
/// trail) — content tables like BlogPost/Comment want a compact int key
/// and "who last changed this."
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }
}
