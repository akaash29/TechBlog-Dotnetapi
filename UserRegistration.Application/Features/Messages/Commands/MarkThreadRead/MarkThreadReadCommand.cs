using MediatR;

namespace UserRegistration.Application.Features.Messages.Commands.MarkThreadRead;

public sealed record MarkThreadReadCommand(Guid OtherUserId) : IRequest;
