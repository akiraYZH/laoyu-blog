namespace laoyu_blog_backend.Options;

public sealed class S3StorageOptions
{
    public const string SectionName = "S3Storage";

    public string BucketName { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string KeyPrefix { get; init; } = "uploads";
}