namespace laoyu_blog_backend.Models;

public class BlogPost
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public ICollection<Category> Categories { get; set; }
     = new List<Category>();

    public string Content { get; set; } = string.Empty;

    public BlogPostStatus Status { get; set; }
        = BlogPostStatus.Draft;

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool Publish()
    {
        if (Status == BlogPostStatus.Published
            && PublishedAtUtc is not null)
        {
            return false;
        }

        Status = BlogPostStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;

        return true;
    }

    public bool Unpublish()
    {
        if (Status == BlogPostStatus.Draft
            && PublishedAtUtc is null)
        {
            return false;
        }

        Status = BlogPostStatus.Draft;
        PublishedAtUtc = null;

        return true;
    }
}