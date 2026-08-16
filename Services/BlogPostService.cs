
using laoyu_blog_backend.Data;
using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace laoyu_blog_backend.Services
{

    public class BlogPostService
    {
        private readonly AppDbContext _dbContext;

        public BlogPostService(AppDbContext appDbContext)
        {
            _dbContext = appDbContext;
        }

        public async Task<PagedResultDto<BlogPostResponseDto>> GetPostsAsync(int page, int pageSize)
        {

            var query = _dbContext.BlogPosts
                .AsNoTracking()
                .OrderByDescending(post => post.CreatedAtUtc)
                .ThenByDescending(post => post.Id);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(post => new BlogPostResponseDto
                {
                    Id = post.Id,
                    Slug = post.Slug,
                    Title = post.Title,
                    Content = post.Content,
                    CreatedAtUtc = post.CreatedAtUtc
                })
                .ToListAsync();

            var result = new PagedResultDto<BlogPostResponseDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / (double)pageSize)
            };

            return result;

        }


        public async Task<BlogPostResponseDto?> GetPostAsync(int id)
        {
            var post = await _dbContext.BlogPosts
                .AsNoTracking()
                .Select(post => new BlogPostResponseDto
                {
                    Id = post.Id,
                    Slug = post.Slug,
                    Title = post.Title,
                    Content = post.Content,
                    CreatedAtUtc = post.CreatedAtUtc
                })
                .FirstOrDefaultAsync(post => post.Id == id);


            return post;
        }

        public async Task<BlogPostResponseDto?> GetPostAsync(string slug)
        {
            var post = await _dbContext.BlogPosts
                .AsNoTracking()
                .Select(post => new BlogPostResponseDto
                {
                    Id = post.Id,
                    Slug = post.Slug,
                    Title = post.Title,
                    Content = post.Content,
                    CreatedAtUtc = post.CreatedAtUtc
                })
                .FirstOrDefaultAsync(post => post.Slug == slug);

            return post;
        }


        public async Task<BlogPostResponseDto> CreatePostAsync(BlogPost blogPost)
        {
            await _dbContext.BlogPosts.AddAsync(blogPost);
            await _dbContext.SaveChangesAsync();

            return new BlogPostResponseDto
            {
                Id = blogPost.Id,
                Slug = blogPost.Slug,
                Title = blogPost.Title,
                Content = blogPost.Content,
                CreatedAtUtc = blogPost.CreatedAtUtc
            };
        }

        public async Task<BlogPostResponseDto?> UpdatePostAsync(int id, BlogPostDto blogPost)
        {
            var post = await _dbContext.BlogPosts
        .FirstOrDefaultAsync(post => post.Id == id);

            if (post is null)
            {
                return null;
            }

            post.Title = blogPost.Title;
            post.Slug = blogPost.Slug;
            post.Content = blogPost.Content;

            await _dbContext.SaveChangesAsync();

            return new BlogPostResponseDto
            {
                Id = post.Id,
                Slug = post.Slug,
                Title = post.Title,
                Content = post.Content,
                CreatedAtUtc = post.CreatedAtUtc
            };

        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _dbContext.BlogPosts
                .FindAsync(id);

            if (post is null) return false;

            _dbContext.BlogPosts.Remove(post);

            await _dbContext.SaveChangesAsync();

            return true;

        }

    }
}