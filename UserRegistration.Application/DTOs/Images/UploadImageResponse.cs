namespace UserRegistration.Application.DTOs.Images;

public sealed class UploadImageResponse
{
    public int Id { get; set; }

    public string ImagePath { get; set; } = string.Empty;
}
