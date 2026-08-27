using System.ComponentModel.DataAnnotations;

namespace laoyu_blog_backend.Dtos;
public class UploadImageRequestDto
{
    [Required]
    public required IFormFile File { get; init; }
}

public sealed class UploadImageResponseDto
{
    public required string Url { get; init; }
}