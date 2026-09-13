using Amazon.S3;
using Amazon.S3.Model;
using laoyu_blog_backend.Options;
using Microsoft.AspNetCore.Http;

namespace laoyu_blog_backend.Services;

public sealed class S3ImageStorageService : IImageStorageService
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedImageTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;

    public S3ImageStorageService(
        IAmazonS3 s3Client,
        S3StorageOptions options)
    {
        _s3Client = s3Client;
        _options = options;
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

        if (!AllowedImageTypes.TryGetValue(
                extension,
                out var contentType))
        {
            return ImageUploadResult.Failure(
                "Only JPG, PNG, GIF, and WebP images are supported.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";

        var keyPrefix = _options.KeyPrefix.Trim('/');

        var objectKey = $"{keyPrefix}/{fileName}";

        await using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(
            request,
            cancellationToken);

        return ImageUploadResult.Success(
            $"/api/images/{fileName}");
    }

    public async Task<string?> CreateReadUrlAsync(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(fileName)
            || fileName != Path.GetFileName(fileName)
            || !AllowedImageTypes.ContainsKey(extension))
        {
            return null;
        }

        var keyPrefix = _options.KeyPrefix.Trim('/');

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = $"{keyPrefix}/{fileName}",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Verb = HttpVerb.GET
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }
}