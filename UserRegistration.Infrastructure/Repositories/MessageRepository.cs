using Microsoft.EntityFrameworkCore;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Messages;
using UserRegistration.Domain.Entities;
using UserRegistration.Infrastructure.Persistence;

namespace UserRegistration.Infrastructure.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MessageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default) =>
        await _dbContext.Messages.AddAsync(message, cancellationToken);

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // Grouped in memory rather than in SQL: there's no Conversation table to
        // group by, and EF Core can't translate "first row per group" without
        // raw SQL/window functions. At this app's scale (a handful of users,
        // at most a few hundred messages each) pulling everything the caller is
        // party to and grouping client-side is simpler and plenty fast.
        var involved = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == userId || m.RecipientId == userId)
            .Select(m => new
            {
                OtherUserId = m.SenderId == userId ? m.RecipientId : m.SenderId,
                m.SenderId,
                m.RecipientId,
                m.Text,
                m.AttachmentFileName,
                HasVoiceNote = m.VoiceNoteUrl != null,
                m.CreatedDate,
                m.IsRead,
            })
            .ToListAsync(cancellationToken);

        var grouped = involved
            .GroupBy(m => m.OtherUserId)
            .Select(g =>
            {
                var last = g.OrderByDescending(m => m.CreatedDate).First();
                return new
                {
                    OtherUserId = g.Key,
                    Last = last,
                    UnreadCount = g.Count(m => m.RecipientId == userId && !m.IsRead),
                };
            })
            .OrderByDescending(g => g.Last.CreatedDate)
            .ToList();

        var otherUserIds = grouped.Select(g => g.OtherUserId).ToList();
        var otherUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(u => otherUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.ProfileImagePath })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return grouped.Select(g =>
        {
            otherUsers.TryGetValue(g.OtherUserId, out var user);
            return new ConversationDto
            {
                OtherUserId = g.OtherUserId,
                OtherUserName = user is null ? "Unknown" : $"{user.FirstName} {user.LastName}",
                OtherUserProfileImagePath = user?.ProfileImagePath,
                LastMessagePreview = BuildPreview(g.Last.Text, g.Last.AttachmentFileName, g.Last.HasVoiceNote),
                LastMessageAt = g.Last.CreatedDate,
                LastMessageIsMine = g.Last.SenderId == userId,
                UnreadCount = g.UnreadCount,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<MessageDto>> GetThreadAsync(
        Guid userId, Guid otherUserId, int take = 50, CancellationToken cancellationToken = default)
    {
        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m =>
                (m.SenderId == userId && m.RecipientId == otherUserId) ||
                (m.SenderId == otherUserId && m.RecipientId == userId))
            .OrderByDescending(m => m.CreatedDate)
            .Take(take)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                SenderProfileImagePath = m.Sender.ProfileImagePath,
                RecipientId = m.RecipientId,
                Text = m.Text,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentFileName = m.AttachmentFileName,
                AttachmentContentType = m.AttachmentContentType,
                AttachmentSizeBytes = m.AttachmentSizeBytes,
                VoiceNoteUrl = m.VoiceNoteUrl,
                VoiceNoteDurationSeconds = m.VoiceNoteDurationSeconds,
                IsRead = m.IsRead,
                CreatedDate = m.CreatedDate,
            })
            .ToListAsync(cancellationToken);

        messages.Reverse(); // oldest first — reading order
        return messages;
    }

    public Task MarkThreadAsReadAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default) =>
        _dbContext.Messages
            .Where(m => m.RecipientId == userId && m.SenderId == otherUserId && !m.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadDate, DateTime.UtcNow), cancellationToken);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Messages.CountAsync(m => m.RecipientId == userId && !m.IsRead, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private static string? BuildPreview(string? text, string? attachmentFileName, bool hasVoiceNote)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text.Length > 80 ? text[..80] + "…" : text;
        }
        if (hasVoiceNote)
        {
            return "Voice message";
        }
        return attachmentFileName is not null ? $"Attachment: {attachmentFileName}" : null;
    }
}
