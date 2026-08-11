using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using laoyu_blog_backend.Data;
using laoyu_blog_backend.Models;
using Microsoft.EntityFrameworkCore;
using laoyu_blog_backend.Dtos;

namespace laoyu_blog_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        public BlogsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static bool IsSlugConflict(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == "IX_BlogPosts_Slug";
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<BlogPost>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<BlogPost>>> GetPosts([FromQuery] PaginationQueryDto pagination)
        {
            var query = _dbContext.BlogPosts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAtUtc)
            .ThenByDescending(post => post.Id);

            var TotalItems = await query.CountAsync();

            var items = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var result = new PagedResultDto<BlogPost>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalItems = TotalItems,
                TotalPages = (int)Math.Ceiling((double)TotalItems / (double)pagination.PageSize)
            };

            return Ok(result);

        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPost>> GetPost(int id)
        {
            var post = await _dbContext.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(post => post.Id == id);

            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpGet("by-slug/{slug}")]
        [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPost>> GetPostBySlug(string slug)
        {
            var post = await _dbContext.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(post => post.Slug == slug);

            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpPost]
        [ProducesResponseType(typeof(BlogPost), StatusCodes.Status201Created)]
        public async Task<ActionResult<BlogPost>> CreatePost([FromBody] BlogPostDto dto)
        {
            var post = new BlogPost
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Content = dto.Content
            };

            await _dbContext.BlogPosts.AddAsync(post);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
                when (IsSlugConflict(exception))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Slug already exists.",
                    Detail = $"The slug '{dto.Slug}' is already in use.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return CreatedAtAction(
                nameof(GetPost),
                new { id = post.Id },
                post);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(BlogPost), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BlogPost>> UpdatePost(int id, [FromBody] BlogPostDto dto)
        {
            var post = await _dbContext.BlogPosts
                .FirstOrDefaultAsync(post => post.Id == id);

            if (post is null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Title))
            {
                post.Title = dto.Title;
            }

            if (!string.IsNullOrEmpty(dto.Slug))
            {
                post.Slug = dto.Slug;
            }

            if (!string.IsNullOrEmpty(dto.Content))
            {
                post.Content = dto.Content;
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
                when (IsSlugConflict(exception))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Slug already exists.",
                    Detail = $"The slug '{dto.Slug}' is already in use.",
                    Status = StatusCodes.Status409Conflict
                });
            }


            return Ok(post);

        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeletePost(int id)
        {
            var post = await _dbContext.BlogPosts
                .FindAsync(id);


            if (post is null)
            {
                return NotFound();
            }

            _dbContext.BlogPosts.Remove(post);

            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

    }
}
