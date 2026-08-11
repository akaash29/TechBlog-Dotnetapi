namespace UserRegistration.Application.DTOs.Messages;

public sealed class MessageDto
{
    public int Id { get; set; }

    public Guid SenderId { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string? SenderProfileImagePath { get; set; }

    public Guid RecipientId { get; set; }

    public string? Text { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    public string? VoiceNoteUrl { get; set; }

    public int? VoiceNoteDurationSeconds { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }
}
