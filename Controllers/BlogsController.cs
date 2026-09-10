using Microsoft.AspNetCore.Mvc;
using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Services;
using Microsoft.AspNetCore.Authorization;

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
            var includeDrafts = User.IsInRole("Admin");

            var result = await _blogPostService.GetPostsAsync(
                pagination.Page,
                pagination.PageSize,
                pagination.CategorySlug,
                includeDrafts);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostResponseDto>> GetPost(int id)
        {
            var includeDrafts = User.IsInRole("Admin");

            var post = await _blogPostService.GetPostAsync(
                id,
                includeDrafts);

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
            var includeDrafts = User.IsInRole("Admin");

            var post = await _blogPostService.GetPostAsync(
                slug,
                includeDrafts);


            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/publish")]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostResponseDto>> PublishPost(int id)
        {
            var publishedPost = await _blogPostService.PublishPostAsync(id);

            if (publishedPost is null)
            {
                return NotFound();
            }

            return Ok(publishedPost);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/unpublish")]
        [ProducesResponseType(typeof(BlogPostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPostResponseDto>> UnpublishPost(int id)
        {
            var unpublishedPost = await _blogPostService.UnpublishPostAsync(id);

            if (unpublishedPost is null)
            {
                return NotFound();
            }

            return Ok(unpublishedPost);
        }

        [Authorize(Roles = "Admin")]
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
