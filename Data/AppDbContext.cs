using Microsoft.EntityFrameworkCore;
using laoyu_blog_backend.Models;

namespace laoyu_blog_backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
    : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
}