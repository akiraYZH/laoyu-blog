---
title: "自动创建博客分类并在数据库中筛选文章"
description: "把分类解析放入 CategoryService，在创建或更新文章时复用已有分类并创建缺失分类，同时让分页查询支持 Category Slug。"
tags:
  - ASP.NET Core
  - EF Core
  - Service Layer
  - Category
  - Filtering
---

# 自动创建博客分类并在数据库中筛选文章

数据库已经具有 Category 多对多关系，但客户端需要先选择已有分类，也可能输入一个新分类。Controller 不应该逐项查询和创建分类；这个工作流由 `CategoryService` 负责。

本文完成三件互相关联的事：获取分类列表、创建文章时自动解析分类，以及通过 Category Slug 在数据库中筛选文章。

## 创建 CategoryService

新建 `Services/CategoryService.cs`，构造函数注入 `AppDbContext`：

```csharp
public sealed class CategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
```

在 `Program.cs` 注册：

```csharp
builder.Services.AddScoped<CategoryService>();
```

`Scoped` 表示一个 HTTP Request 使用同一个 Service 实例，并与同一 Scope 中的 `AppDbContext` 协作。

## 返回已有分类

```csharp
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
```

读取列表不需要变更追踪，因此使用 `AsNoTracking`；`Select` 直接在数据库查询中投影为 Response DTO。

## 解析并创建缺失分类

Service 对输入名称执行以下流程：

```text
Trim 和去除空项
    ↓
忽略大小写去重
    ↓
为名称生成 Slug
    ↓
一次查询所有已存在分类
    ↓
只创建缺失分类
    ↓
返回现有分类和新分类的合集
```

对外方法：

```csharp
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
```

`AddRange` 是 `DbSet`/Collection 提供的方法，用于一次加入多个 Entity；它不是特殊语法。这里不立即调用 `SaveChangesAsync`，由创建或更新文章的完整业务流程统一提交。

名称到 Slug 的转换应集中在同一个方法，避免 Create 和 Update 产生不同规则。例如：

```csharp
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
```

Category Slug 的 Unique Index 仍是并发下的最终保护。应用层先查询不能替代数据库约束。

## 接入 BlogPostService

构造函数增加依赖：

```csharp
private readonly CategoryService _categoryService;

public BlogPostService(
    AppDbContext dbContext,
    CategoryService categoryService)
{
    _dbContext = dbContext;
    _categoryService = categoryService;
}
```

创建文章时：

```csharp
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
```

更新文章时必须加载当前关系，然后替换关联集合：

```csharp
var post = await _dbContext.BlogPosts
    .Include(post => post.Categories)
    .FirstOrDefaultAsync(post => post.Id == id);

post.Categories.Clear();

foreach (var category in categories)
{
    post.Categories.Add(category);
}

await _dbContext.SaveChangesAsync();
```

`Include` 让 EF Core 知道当前关联，才能正确删除旧的 Join Table Rows 并加入新关系。

## 创建分类列表接口

新建 `Controllers/CategoriesController.cs`：

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponseDto>>>
        GetCategories()
    {
        return Ok(await _categoryService.GetAllAsync());
    }
}
```

Controller 只处理 HTTP，查询和 DTO Mapping 留在 Service。

## 给分页 Query 增加分类参数

在 `PaginationQueryDto` 中加入：

```csharp
[StringLength(
    100,
    ErrorMessage = "Category slug cannot exceed 100 characters.")]
public string? CategorySlug { get; set; }
```

Controller 传给 Service：

```csharp
await _blogPostService.GetPostsAsync(
    pagination.Page,
    pagination.PageSize,
    pagination.CategorySlug);
```

Service 必须在 `CountAsync`、排序和分页之前执行 `Where`：

```csharp
var query = _dbContext.BlogPosts.AsNoTracking();

if (!string.IsNullOrWhiteSpace(categorySlug))
{
    query = query.Where(post =>
        post.Categories.Any(category =>
            category.Slug == categorySlug));
}

var totalItems = await query.CountAsync();

var items = await query
    .OrderByDescending(post => post.CreatedAtUtc)
    .ThenByDescending(post => post.Id)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(/* BlogPostResponseDto projection */)
    .ToListAsync();
```

如果只在前端当前页过滤，`TotalItems`、`TotalPages` 和后续页面都会错误。

## 验证

创建一篇带有一个已有分类和一个新分类的文章：

```json
{
  "title": "Category Test",
  "slug": "category-test",
  "content": "Testing categories.",
  "categoryNames": ["ASP.NET Core", "PostgreSQL"]
}
```

然后验证：

```http
GET /api/categories
GET /api/blogs?page=1&pageSize=10&categorySlug=asp-net-core
```

预期：分类列表没有重复项，筛选结果只包含关联文章，并且分页统计基于筛选后的完整 Query。

## 常见错误

### `return null` 无法表达缺失了哪些分类

当前设计不把缺失分类当作错误，而是创建缺失项，因此无需用 `null` 让 Controller 猜测失败原因。需要拒绝新分类时，应返回明确 Result 或抛出具体业务异常。

### Service 自己先 SaveChanges

分类解析只是创建文章工作流的一部分。让外层 BlogPost Service 一次提交，能避免分类成功但文章失败后留下不完整业务状态。

## 完成状态

```text
CategoryService → 获取、规范化和创建分类
BlogPostService → 把分类接入 Create、Update 和 Response
CategoriesController → 返回分类列表
GET /api/blogs?categorySlug=... → 在数据库中筛选后分页
```

## 参考资料

- [EF Core Related Data](https://learn.microsoft.com/ef/core/querying/related-data)
- [EF Core Relationships](https://learn.microsoft.com/ef/core/modeling/relationships)

## 主线导航

- 上一步：[使用 EF Core 为博客文章加入多分类](./14a-model-blog-categories-many-to-many.md)
- 下一步：[为 Markdown 编辑器建立本地图片上传 API](./14c-build-local-image-upload-api.md)
