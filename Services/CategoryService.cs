using System.Text.RegularExpressions;
using laoyu_blog_backend.Data;
using laoyu_blog_backend.Dtos;
using laoyu_blog_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace laoyu_blog_backend.Services;

public sealed class CategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<CategoryResponseDto>> GetAllAsync()
    {
        return _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug
            })
            .ToListAsync();
    }

    public async Task<List<Category>> GetOrCreateAsync(
        IEnumerable<string> categoryNames)
    {
        var candidates = BuildCandidates(categoryNames);

        if (candidates.Count == 0)
        {
            return [];
        }

        var existingCategories = await FindExistingAsync(
            candidates);

        var newCategories = CreateMissingCategories(
            candidates,
            existingCategories);

        _dbContext.Categories.AddRange(newCategories);

        return existingCategories
            .Concat(newCategories)
            .ToList();
    }

    private static List<CategoryCandidate> BuildCandidates(
        IEnumerable<string> categoryNames)
    {
        return categoryNames
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new CategoryCandidate(name, CreateSlug(name)))
            .GroupBy(candidate => candidate.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private Task<List<Category>> FindExistingAsync(
        List<CategoryCandidate> candidates)
    {
        var slugs = candidates
            .Select(candidate => candidate.Slug)
            .ToList();

        return _dbContext.Categories
            .Where(category => slugs.Contains(category.Slug))
            .ToListAsync();
    }

    private static List<Category> CreateMissingCategories(
        List<CategoryCandidate> candidates,
        List<Category> existingCategories)
    {
        var existingSlugs = existingCategories
            .Select(category => category.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates
            .Where(candidate =>
                !existingSlugs.Contains(candidate.Slug))
            .Select(candidate => new Category
            {
                Name = candidate.Name,
                Slug = candidate.Slug
            })
            .ToList();
    }

    private static string CreateSlug(string name)
    {
        var slugSource = name
            .Trim()
            .ToLowerInvariant()
            .Replace("#", " sharp ")
            .Replace("+", " plus ");

        return Regex.Replace(
                slugSource,
                @"[^\p{L}\p{N}]+",
                "-")
            .Trim('-');
    }

    private sealed record CategoryCandidate(
        string Name,
        string Slug);
}
