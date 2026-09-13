using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace laoyu_blog_backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api")]
public class UploadsController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;

    public UploadsController(
        IImageStorageService imageStorageService)
    {
        _imageStorageService = imageStorageService;
    }

    [AllowAnonymous]
    [HttpGet("images/{fileName}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string fileName)
    {
        var imageUrl =
            await _imageStorageService.CreateReadUrlAsync(fileName);

        if (imageUrl is null)
        {
            return NotFound();
        }

        return Redirect(imageUrl);
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
        typeof(UploadImageResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadImageResponseDto>> UploadImage(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await _imageStorageService.SaveAsync(
            file,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage
            });
        }

        return Ok(new UploadImageResponseDto
        {
            Url = result.Url!
        });
    }
}