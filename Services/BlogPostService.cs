
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
        private readonly ILogger<BlogPostService> _logger;

        public BlogPostService(
            AppDbContext appDbContext,
            CategoryService categoryService,
            ILogger<BlogPostService> logger)
        {
            _dbContext = appDbContext;
            _categoryService = categoryService;
            _logger = logger;
        }

        private IQueryable<BlogPost> BuildReadablePostsQuery(bool includeDrafts)
        {
            var query = _dbContext.BlogPosts
                .AsNoTracking();

            if (!includeDrafts)
            {
                query = query.Where(post =>
                    post.Status == BlogPostStatus.Published);
            }

            return query;
        }

        public async Task<PagedResultDto<BlogPostResponseDto>> GetPostsAsync(
            int page,
            int pageSize,
            string? categorySlug,
            bool includeDrafts)
        {
            var query = BuildReadablePostsQuery(includeDrafts);

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
                    Status = post.Status.ToString(),
                    PublishedAtUtc = post.PublishedAtUtc,
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


        public async Task<BlogPostResponseDto?> GetPostAsync(
            int id,
            bool includeDrafts)
        {
            var post = await BuildReadablePostsQuery(includeDrafts)
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
                    Status = post.Status.ToString(),
                    PublishedAtUtc = post.PublishedAtUtc,
                    CreatedAtUtc = post.CreatedAtUtc
                })
                .FirstOrDefaultAsync(post => post.Id == id);


            return post;
        }

        public async Task<BlogPostResponseDto?> GetPostAsync(
            string slug,
            bool includeDrafts)
        {
            var post = await BuildReadablePostsQuery(includeDrafts)
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
                    Status = post.Status.ToString(),
                    PublishedAtUtc = post.PublishedAtUtc,
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

            _logger.LogInformation(
                "Blog post {PostId} created.",
                blogPost.Id);

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
                Status = blogPost.Status.ToString(),
                PublishedAtUtc = blogPost.PublishedAtUtc,
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
                _logger.LogWarning(
                    "Blog post {PostId} was not found for update.",
                    id);

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

            _logger.LogInformation(
                "Blog post {PostId} updated.",
                post.Id);

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
                Status = post.Status.ToString(),
                PublishedAtUtc = post.PublishedAtUtc,
                CreatedAtUtc = post.CreatedAtUtc
            };

        }

        public async Task<BlogPostResponseDto?> PublishPostAsync(int id)
        {
            var post = await _dbContext.BlogPosts
                .Include(post => post.Categories)
                .FirstOrDefaultAsync(post => post.Id == id);

            if (post is null)
            {
                _logger.LogWarning(
                    "Blog post {PostId} was not found for publishing.",
                    id);

                return null;
            }

            if (post.Publish())
            {
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Blog post {PostId} published.",
                    post.Id);
            }

            return new BlogPostResponseDto
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
                Status = post.Status.ToString(),
                PublishedAtUtc = post.PublishedAtUtc,
                CreatedAtUtc = post.CreatedAtUtc
            };
        }

        public async Task<BlogPostResponseDto?> UnpublishPostAsync(int id)
        {
            var post = await _dbContext.BlogPosts
                .Include(post => post.Categories)
                .FirstOrDefaultAsync(post => post.Id == id);

            if (post is null)
            {
                _logger.LogWarning(
                    "Blog post {PostId} was not found for unpublishing.",
                    id);

                return null;
            }

            if (post.Unpublish())
            {
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Blog post {PostId} unpublished.",
                    post.Id);
            }

            return new BlogPostResponseDto
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
                Status = post.Status.ToString(),
                PublishedAtUtc = post.PublishedAtUtc,
                CreatedAtUtc = post.CreatedAtUtc
            };
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _dbContext.BlogPosts
                .FindAsync(id);

            if (post is null)
            {
                _logger.LogWarning(
                    "Blog post {PostId} was not found for deletion.",
                    id);

                return false;
            }

            _dbContext.BlogPosts.Remove(post);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Blog post {PostId} deleted.",
                post.Id);

            return true;

        }
    }


}