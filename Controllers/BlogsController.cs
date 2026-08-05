using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BlogPost>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BlogPost>>> GetPosts()
        {
            var posts = await _dbContext.BlogPosts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAtUtc)
            .ToListAsync();

            return Ok(posts);
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

        [HttpPost]
        [ProducesResponseType(typeof(BlogPost), StatusCodes.Status201Created)]
        public async Task<ActionResult<BlogPost>> CreatePost([FromBody] CreateBlogPostDto dto)
        {
            var post = new BlogPost
            {
                Title = dto.Title,
                Content = dto.Content
            };

            await _dbContext.BlogPosts.AddAsync(post);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetPost),
                new { id = post.Id },
                post);
        }
    }
}
