namespace UserRegistration.Application.Common.Constants;

public static class ImageUploadConstraints
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    public static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];
}
