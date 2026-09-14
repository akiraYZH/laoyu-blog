namespace laoyu_blog_backend.Dtos;

public class BlogPostResponseDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public List<CategoryResponseDto> Categories { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public List<string> Tags { get; set; } = [];
    public int Order { get; set; }
}