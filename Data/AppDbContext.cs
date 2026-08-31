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
    public DbSet<Category> Categories =>
    Set<Category>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
        .HasIndex(category => category.Slug)
        .IsUnique();

        modelBuilder.Entity<BlogPost>()
        .HasIndex(post => post.Slug)
        .IsUnique();

        modelBuilder.Entity<BlogPost>()
        .HasMany(post => post.Categories)
        .WithMany(category => category.BlogPosts)
        .UsingEntity("BlogPostCategories");
    }
}