namespace UserRegistration.Domain.Entities;

/// <summary>
/// Master/lookup table of roles a <see cref="User"/> can hold. Rows are fixed, seeded data
/// (see RoleConfiguration) rather than user-managed records, so this uses a small stable int
/// key instead of the Guid + audit columns the rest of the domain's entities carry.
/// </summary>
public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
