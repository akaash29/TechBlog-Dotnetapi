namespace UserRegistration.Domain.Entities;

/// <summary>
/// A single direct message between two users. There's no separate
/// "Conversation" entity — a conversation is just the set of messages
/// between a (Sender, Recipient) pair, grouped by the other participant at
/// query time (see IMessageRepository.GetConversationsAsync). A message
/// carries text and/or one attachment and/or one voice note — at least one
/// of the three (see AddMessageCommandValidator).
/// </summary>
public class Message
{
    public int Id { get; set; }

    public Guid SenderId { get; set; }

    public User Sender { get; set; } = null!;

    public Guid RecipientId { get; set; }

    public User Recipient { get; set; } = null!;

    public string? Text { get; set; }

    /// <summary>Blob URL of an attached file (max 5 MB — see MessageAttachmentConstraints).</summary>
    public string? AttachmentUrl { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    /// <summary>Blob URL of a recorded voice note — same upload pipeline and size
    /// cap as a file attachment, just a distinct field so the UI can tell them apart.</summary>
    public string? VoiceNoteUrl { get; set; }

    public int? VoiceNoteDurationSeconds { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
