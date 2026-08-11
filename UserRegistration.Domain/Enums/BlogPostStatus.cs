namespace UserRegistration.Domain.Enums;

/// <summary>
/// A post's place in the editorial workflow. Writers/editors publishing a
/// post land in PendingApproval until an admin reviews it; an admin
/// publishing their own post skips straight to Published (see
/// CreateBlogPostCommandHandler). There's no persisted "Rejected" state —
/// rejecting a pending post deletes it outright, blob storage files
/// included (see RejectBlogPostCommandHandler).
/// </summary>
public enum BlogPostStatus
{
    Draft = 0,
    PendingApproval = 1,
    Published = 2,
}
