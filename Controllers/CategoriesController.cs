using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Services;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet("{slug}/tags")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetCategoryTags(
        string slug,
        [FromServices] BlogPostService blogPostService)
    {
        var includeDrafts = User.IsInRole("Admin");
        var tags = await blogPostService.GetCategoryTagsAsync(slug, includeDrafts);
        return Ok(tags);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponseDto>> CreateCategory([FromBody] CategoryDto dto)
    {
        var category = await _categoryService.CreateCategoryAsync(dto);
        if (category == null) return Conflict("Category already exists or name is invalid.");
        
        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponseDto>> UpdateCategory(int id, [FromBody] CategoryDto dto)
    {
        var category = await _categoryService.UpdateCategoryAsync(id, dto);
        if (category == null) return Conflict("Category not found, name is invalid, or slug already exists.");
        
        return Ok(category);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var success = await _categoryService.DeleteCategoryAsync(id);
        if (!success) return NotFound();

        return NoContent();
    }
}
