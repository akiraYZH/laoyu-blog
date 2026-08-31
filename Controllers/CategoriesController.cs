using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace laoyu_blog_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(List<CategoryResponseDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetCategories()
    {
        return Ok(await _categoryService.GetAllAsync());
    }
}
