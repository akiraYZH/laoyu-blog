using System.ComponentModel.DataAnnotations;

namespace laoyu_blog_backend.Dtos;

public class BlogPostDto
{
    [Required(ErrorMessage = "Title is required!")]
    [StringLength(
        200,
        MinimumLength = 1,
        ErrorMessage = "Title must be between 1 and 200 characters.")
    ]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(100,
        MinimumLength = 3,
        ErrorMessage = "Slug must be between 3 and 100 characters.")
    ]
    [RegularExpression(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug can only contain lowercase letters, numbers, and single hyphens."
    )]
    public string Slug { get; set; } = string.Empty;



    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}