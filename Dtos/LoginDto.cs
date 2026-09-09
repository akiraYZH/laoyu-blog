using System.ComponentModel.DataAnnotations;

namespace laoyu_blog_backend.Dtos.Auth;

public sealed class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}