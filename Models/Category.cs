namespace laoyu_blog_backend.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public ICollection<BlogPost> BlogPosts { get; set; }
        = new List<BlogPost>();
}