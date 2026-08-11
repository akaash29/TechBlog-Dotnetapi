namespace UserRegistration.Application.DTOs.Messages;

public sealed class UploadMessageAttachmentResponse
{
    public string Url { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
