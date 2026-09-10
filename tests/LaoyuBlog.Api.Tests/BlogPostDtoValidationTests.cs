using System.ComponentModel.DataAnnotations;
using laoyu_blog_backend.Dtos;

namespace LaoyuBlog.Api.Tests;

public sealed class BlogPostDtoValidationTests
{
    [Fact]
    public void Validate_EmptySlug_ReturnsRequiredError()
    {
        var dto = CreateValidDto();
        dto.Slug = string.Empty;

        var errors = Validate(dto);

        Assert.Contains(
            errors,
            error =>
                error.ErrorMessage == "Slug is required."
                && error.MemberNames.Contains(
                    nameof(BlogPostDto.Slug)));
    }

    [Fact]
    public void Validate_ShortSlug_ReturnsLengthError()
    {
        var dto = CreateValidDto();
        dto.Slug = "ab";

        var errors = Validate(dto);

        Assert.Contains(
            errors,
            error =>
                error.ErrorMessage
                    == "Slug must be between 3 and 100 characters."
                && error.MemberNames.Contains(
                    nameof(BlogPostDto.Slug)));
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsRequiredError()
    {
        var dto = CreateValidDto();
        dto.Title = string.Empty;

        var errors = Validate(dto);

        Assert.Contains(
            errors,
            error =>
                error.ErrorMessage == "Title is required!"
                && error.MemberNames.Contains(
                    nameof(BlogPostDto.Title)));
    }

    [Fact]
    public void Validate_MoreThanTenCategories_ReturnsMaximumError()
    {
        var dto = CreateValidDto();
        dto.CategoryNames = Enumerable.Range(1, 11)
            .Select(index => $"Category {index}")
            .ToList();

        var errors = Validate(dto);

        Assert.Contains(
            errors,
            error =>
                error.ErrorMessage
                    == "A blog post can have at most 10 categories."
                && error.MemberNames.Contains(
                    nameof(BlogPostDto.CategoryNames)));
    }

    [Fact]
    public void Validate_InvalidCategoryName_ReturnsCategoryError()
    {
        var dto = CreateValidDto();
        dto.CategoryNames = ["---"];

        var errors = Validate(dto);

        Assert.Contains(
            errors,
            error =>
                error.ErrorMessage
                    == "Each category must be 1-50 characters and contain a letter or number."
                && error.MemberNames.Contains(
                    nameof(BlogPostDto.CategoryNames)));
    }

    private static BlogPostDto CreateValidDto()
    {
        return new BlogPostDto
        {
            Title = "Valid title",
            Slug = "valid-slug",
            Content = "Valid content",
            CategoryNames = ["ASP.NET Core"]
        };
    }

    private static IReadOnlyList<ValidationResult> Validate(
        BlogPostDto dto)
    {
        var errors = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            errors,
            validateAllProperties: true);

        return errors;
    }
}
