using MediatR;

namespace UserRegistration.Application.Features.Messages.Queries.GetUnreadMessageCount;

public sealed record GetUnreadMessageCountQuery : IRequest<int>;
