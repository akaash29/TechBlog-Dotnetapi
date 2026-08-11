using MediatR;
using UserRegistration.Application.DTOs.Messages;

namespace UserRegistration.Application.Features.Messages.Queries.GetMessageThread;

public sealed record GetMessageThreadQuery(Guid OtherUserId, int Take = 50) : IRequest<IReadOnlyList<MessageDto>>;
