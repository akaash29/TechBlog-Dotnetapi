namespace UserRegistration.Application.Common.Constants;

public static class MessageAttachmentConstraints
{
    /// <summary>Applies to both a file attachment and a recorded voice note.</summary>
    public const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    public static readonly string[] AllowedFileContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "application/zip",
    ];

    // MediaRecorder's default output — codec support (and therefore the
    // exact mime type it produces) varies by browser.
    public static readonly string[] AllowedVoiceContentTypes =
    [
        "audio/webm",
        "audio/ogg",
        "audio/mp4",
        "audio/mpeg",
        "audio/wav",
    ];
}
