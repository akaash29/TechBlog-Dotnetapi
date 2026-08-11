using MediatR;
using UserRegistration.Application.Common.Constants;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Messages;
using UserRegistration.Domain.Entities;

namespace UserRegistration.Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed class UploadMessageAttachmentCommandHandler
    : IRequestHandler<UploadMessageAttachmentCommand, UploadMessageAttachmentResponse>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UploadMessageAttachmentCommandHandler(
        IBlobStorageService blobStorageService,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _blobStorageService = blobStorageService;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UploadMessageAttachmentResponse> Handle(
        UploadMessageAttachmentCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to send an attachment.");

        _ = await _userRepository.GetByIdAsync(request.RecipientId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.RecipientId);

        // Both directions of a conversation land in the same folder — sort the
        // pair rather than using (sender, recipient) order.
        var pairKey = string.CompareOrdinal(senderId.ToString(), request.RecipientId.ToString()) <= 0
            ? $"{senderId:N}_{request.RecipientId:N}"
            : $"{request.RecipientId:N}_{senderId:N}";

        var subfolder = string.Equals(request.Kind, "voice", StringComparison.OrdinalIgnoreCase)
            ? BlobContainers.MessageVoiceFolder
            : BlobContainers.MessageFilesFolder;

        var extension = Path.GetExtension(request.FileName);
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blobPath = $"{pairKey}/{subfolder}/{blobName}";

        var url = await _blobStorageService.UploadAsync(
            BlobContainers.Messages, blobPath, request.Content, request.ContentType, cancellationToken);

        return new UploadMessageAttachmentResponse
        {
            Url = url,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.Length,
        };
    }
}
