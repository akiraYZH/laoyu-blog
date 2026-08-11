namespace laoyu_blog_backend.Dtos;

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public int TotalItems { get; set;}
}