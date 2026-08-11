using AutoMapper;
using MediatR;
using UserRegistration.Application.Common.Constants;
using UserRegistration.Application.Common.Exceptions;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.DTOs.Users;
using UserRegistration.Domain.Enums;

namespace UserRegistration.Application.Features.Users.Commands.UploadProfileImage;

public sealed class UploadProfileImageCommandHandler : IRequestHandler<UploadProfileImageCommand, UserDto>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UploadProfileImageCommandHandler(
        IBlobStorageService blobStorageService,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _blobStorageService = blobStorageService;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UploadProfileImageCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        var callerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload a profile photo.");
        var isAdmin = _currentUserService.IsInRole(nameof(UserRole.Admin));

        if (!isAdmin && callerId != request.UserId)
        {
            throw new ForbiddenException("You can only change your own profile photo.");
        }

        // One virtual folder per user ("{userId:N}/…") inside the shared "userimage"
        // container — groups each user's uploads together without needing a real
        // per-user container (Azure Blob Storage has no concept of nested containers).
        var extension = Path.GetExtension(request.FileName);
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blobPath = $"{request.UserId:N}/{blobName}";

        var imagePath = await _blobStorageService.UploadAsync(
            BlobContainers.UserImage, blobPath, request.Content, request.ContentType, cancellationToken);

        user.SetProfileImage(imagePath);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
