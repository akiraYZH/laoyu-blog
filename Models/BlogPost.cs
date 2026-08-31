namespace laoyu_blog_backend.Models;

public class BlogPost
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

   public ICollection<Category> Categories { get; set; }
    = new List<Category>();

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}