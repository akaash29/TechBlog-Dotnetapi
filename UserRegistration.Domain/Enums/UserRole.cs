namespace UserRegistration.Domain.Enums;

/// <summary>
/// Strongly-typed constants matching the seeded rows in the Roles master table
/// (<see cref="Entities.Role"/>). The underlying int values line up with the seeded
/// Role.Id values, so `(int)UserRole.Admin` is always a valid Roles.Id.
/// </summary>
public enum UserRole
{
    Writer = 1,
    Editor = 2,
    Photographer = 3,
    Reader = 4,
    Admin = 5
}
