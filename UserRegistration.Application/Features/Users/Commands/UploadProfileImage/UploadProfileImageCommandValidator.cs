using FluentValidation;
using UserRegistration.Application.Common.Constants;

namespace UserRegistration.Application.Features.Users.Commands.UploadProfileImage;

public sealed class UploadProfileImageCommandValidator : AbstractValidator<UploadProfileImageCommand>
{
    public UploadProfileImageCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty();

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("The file is empty.")
            .LessThanOrEqualTo(ImageUploadConstraints.MaxSizeBytes)
            .WithMessage($"Images must be {ImageUploadConstraints.MaxSizeBytes / 1024 / 1024} MB or smaller.");

        RuleFor(x => x.ContentType)
            .Must(contentType => ImageUploadConstraints.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Only JPEG, PNG, GIF, and WEBP images are allowed.");
    }
}
