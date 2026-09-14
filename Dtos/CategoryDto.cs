using System.ComponentModel.DataAnnotations;

namespace laoyu_blog_backend.Dtos;

public class CategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 100 characters.")]
    public string Name { get; set; } = string.Empty;
}
