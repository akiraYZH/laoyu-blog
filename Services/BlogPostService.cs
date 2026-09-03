
using laoyu_blog_backend.Data;
using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace laoyu_blog_backend.Services
{

    public class BlogPostService
    {
        private readonly AppDbContext _dbContext;
        private readonly CategoryService _categoryService;

        public BlogPostService(AppDbContext appDbContext, CategoryService categoryService)
        {
            _dbContext = appDbContext;
            _categoryService = categoryService;
        }

        public async Task<PagedResultDto<BlogPostResponseDto>> GetPostsAsync(
            int page,
            int pageSize,
            string? categorySlug)
        {
            var query = _dbContext.BlogPosts
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                query = query.Where(post =>
                    post.Categories.Any(category => category.Slug == categorySlug));
            }

            var orderedQuery = query
                .OrderByDescending(post => post.CreatedAtUtc)
                .ThenByDescending(post => post.Id);

            var totalItems = await query.CountAsync();

            var items = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(post => new BlogPostResponseDto
                {
                    Id = post.Id,
                    Slug = post.Slug,
                    Title = post.Title,
                    Content = post.Content,
                    Categories = post.Categories
                        .OrderBy(category => category.Name)
                        .Select(category => new CategoryResponseDto
                        {
                            Id = category.Id,
                            Name = category.Name,
                            Slug = category.Slug
                        })
                        .ToList(),
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
                    Categories = post.Categories
                        .OrderBy(category => category.Name)
                        .Select(category => new CategoryResponseDto
                        {
                            Id = category.Id,
                            Name = category.Name,
                            Slug = category.Slug
                        })
                        .ToList(),
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
                    Categories = post.Categories
                        .OrderBy(category => category.Name)
                        .Select(category => new CategoryResponseDto
                        {
                            Id = category.Id,
                            Name = category.Name,
                            Slug = category.Slug
                        })
                        .ToList(),
                    CreatedAtUtc = post.CreatedAtUtc
                })
                .FirstOrDefaultAsync(post => post.Slug == slug);

            return post;
        }


        public async Task<BlogPostResponseDto> CreatePostAsync(BlogPostDto dto)
        {
            var categories = await _categoryService.GetOrCreateAsync(
                dto.CategoryNames);

            var blogPost = new BlogPost
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Content = dto.Content,
                Categories = categories
            };

            await _dbContext.BlogPosts.AddAsync(blogPost);
            await _dbContext.SaveChangesAsync();

            return new BlogPostResponseDto
            {
                Id = blogPost.Id,
                Slug = blogPost.Slug,
                Title = blogPost.Title,
                Content = blogPost.Content,
                Categories = categories
                    .OrderBy(category => category.Name)
                    .Select(category => new CategoryResponseDto
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Slug = category.Slug
                    })
                    .ToList(),
                CreatedAtUtc = blogPost.CreatedAtUtc
            };
        }

        public async Task<BlogPostResponseDto?> UpdatePostAsync(int id, BlogPostDto dto)
        {
            var post = await _dbContext.BlogPosts
                .Include(post => post.Categories)
                .FirstOrDefaultAsync(post => post.Id == id);

            if (post is null)
            {
                return null;
            }

            var categories = await _categoryService.GetOrCreateAsync(
                dto.CategoryNames);

            post.Title = dto.Title;
            post.Slug = dto.Slug;
            post.Content = dto.Content;
            post.Categories.Clear();

            foreach (var category in categories)
            {
                post.Categories.Add(category);
            }

            await _dbContext.SaveChangesAsync();

            return new BlogPostResponseDto
            {
                Id = post.Id,
                Slug = post.Slug,
                Title = post.Title,
                Content = post.Content,
                Categories = categories
                    .OrderBy(category => category.Name)
                    .Select(category => new CategoryResponseDto
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Slug = category.Slug
                    })
                    .ToList(),
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