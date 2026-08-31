namespace laoyu_blog_backend.Dtos;

public class BlogPostResponseDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public List<CategoryResponseDto> Categories { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
}