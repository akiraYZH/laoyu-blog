using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace laoyu_blog_backend.Services;

public sealed class LocalImageStorageService : IImageStorageService
{
    private const string UploadsFolder = "uploads";

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    ];

    private readonly IWebHostEnvironment _environment;

    public LocalImageStorageService(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<ImageUploadResult> SaveAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return ImageUploadResult.Failure(
                "Please select an image to upload.");
        }

        if (file.Length > MaxFileSize)
        {
            return ImageUploadResult.Failure(
                "Image size cannot exceed 5 MB.");
        }

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return ImageUploadResult.Failure(
                "Only JPG, PNG, GIF, and WebP images are supported.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot");
        }

        var uploadsDirectory = Path.Combine(
            webRootPath,
            UploadsFolder);

        Directory.CreateDirectory(uploadsDirectory);

        var filePath = Path.Combine(
            uploadsDirectory,
            fileName);

        await using var stream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await file.CopyToAsync(
            stream,
            cancellationToken);

        var imageUrl = $"/{UploadsFolder}/{fileName}";

        return ImageUploadResult.Success(imageUrl);
    }
}