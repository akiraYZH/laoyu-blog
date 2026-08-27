using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace laoyu_blog_backend.Controllers;

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