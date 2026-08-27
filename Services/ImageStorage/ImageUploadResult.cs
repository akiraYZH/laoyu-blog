namespace laoyu_blog_backend.Services;

public sealed class ImageUploadResult
{
    public bool Succeeded { get; init; }

    public string? Url { get; init; }

    public string? ErrorMessage { get; init; }

    public static ImageUploadResult Success(string url)
    {
        return new ImageUploadResult
        {
            Succeeded = true,
            Url = url
        };
    }

    public static ImageUploadResult Failure(string errorMessage)
    {
        return new ImageUploadResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }
}