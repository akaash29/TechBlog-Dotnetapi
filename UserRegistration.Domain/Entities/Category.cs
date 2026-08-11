namespace UserRegistration.Domain.Entities;

/// <summary>
/// Master/lookup table of blog post categories. Rows are fixed, seeded
/// data (see CategoryConfiguration) — same shape as <see cref="Role"/>,
/// a small stable-ish int key with no audit columns.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
