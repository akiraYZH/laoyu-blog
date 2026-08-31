using Microsoft.AspNetCore.Mvc;
using laoyu_blog_backend.Models;
using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Services;

namespace laoyu_blog_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogsController : ControllerBase
    {
        private readonly BlogPostService _blogPostService;

        public BlogsController(BlogPostService blogsService)
        {
            _blogPostService = blogsService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<BlogPostResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<BlogPostResponseDto>>> GetPosts([FromQuery] PaginationQueryDto pagination)
        {
            var result = await _blogPostService.GetPostsAsync(pagination.Page, pagination.PageSize);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostResponseDto>> GetPost(int id)
        {
            var post = await _blogPostService.GetPostAsync(id);

            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpGet("by-slug/{slug}")]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostResponseDto>> GetPostBySlug(string slug)
        {
            var post = await _blogPostService.GetPostAsync(slug);


            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<BlogPostResponseDto>> CreatePost([FromBody] BlogPostDto dto)
        {
            var createdPost = await _blogPostService.CreatePostAsync(dto);

            return CreatedAtAction(
                nameof(GetPost),
                new { id = createdPost.Id },
                createdPost);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostResponseDto>> UpdatePost(int id, [FromBody] BlogPostDto post)
        {
            var result = await _blogPostService.UpdatePostAsync(id, post);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeletePost(int id)
        {
            var isDeleted = await _blogPostService.DeletePostAsync(id);

            if (isDeleted is false) return NotFound();

            return NoContent();
        }

    }
}
