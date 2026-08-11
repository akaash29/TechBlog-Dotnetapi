using MediatR;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Features.Messages.Queries.GetConversations;

public sealed record GetConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;
