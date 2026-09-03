using System.ComponentModel.DataAnnotations;

namespace laoyu_blog_backend.Dtos;

public class PaginationQueryDto
{
    [Range(1, 1_000_000, ErrorMessage = "Page must be between 1 and 1,000,000.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;

    [StringLength(100, ErrorMessage = "Category slug cannot exceed 100 characters.")]
    public string? CategorySlug { get; set; }
}