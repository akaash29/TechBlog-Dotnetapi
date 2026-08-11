namespace UserRegistration.Application.DTOs.Messages;

/// <summary>One row in the conversation list — the other participant, plus
/// a preview of where things left off. There's no Conversation table; this
/// is computed from Messages grouped by the other participant (see
/// IMessageRepository.GetConversationsAsync).</summary>
public sealed class ConversationDto
{
    public Guid OtherUserId { get; set; }

    public string OtherUserName { get; set; } = string.Empty;

    public string? OtherUserProfileImagePath { get; set; }

    public string? LastMessagePreview { get; set; }

    public DateTime LastMessageAt { get; set; }

    public bool LastMessageIsMine { get; set; }

    public int UnreadCount { get; set; }

    public bool IsOnline { get; set; }
}
