---
title: "把 Slug 接入 ASP.NET Core 文章 API"
description: "让 DTO 接收 Slug，在 POST 与 PUT 中完成 Mapping，并增加按 Slug 查询文章的 Endpoint。"
tags:
  - ASP.NET Core
  - EF Core
  - DTO
  - Slug
---

# 把 Slug 接入 ASP.NET Core 文章 API

上一篇已经让数据库中的 `Slug` 成为 `NOT NULL` 且拥有 Unique Index。现在需要让 HTTP API 接收、保存、更新和查询 Slug。

本文暂时只使用不重复的测试值。数据库冲突如何转换成 `409 Conflict`，留给下一篇单独处理。

## 前置状态

Entity 已包含：

```csharp
public string Slug { get; set; } = string.Empty;
```

PostgreSQL 已满足：

```text
Slug NOT NULL
IX_BlogPosts_Slug UNIQUE
```

## 在 DTO 中接收 Slug

```csharp
using System.ComponentModel.DataAnnotations;

public class BlogPostDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    [RegularExpression(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage =
            "Slug can contain lowercase letters, numbers and hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}
```

DTO 负责格式验证；Database Unique Index 负责最终唯一性。

## POST 保存 Slug

```csharp
var post = new BlogPost
{
    Slug = dto.Slug,
    Title = dto.Title,
    Content = dto.Content
};

_dbContext.BlogPosts.Add(post);
await _dbContext.SaveChangesAsync();
```

如果遗漏：

```csharp
Slug = dto.Slug
```

DTO 虽然收到 Slug，但 Entity 仍会使用默认空字符串。DTO 到 Entity 的 Mapping 必须显式完成。

## PUT 更新 Slug

```csharp
post.Slug = dto.Slug;
post.Title = dto.Title;
post.Content = dto.Content;

await _dbContext.SaveChangesAsync();
```

当前接口采用完整 PUT 语义，因此客户端提交所有可更新字段。需要 Partial Update 时，应单独设计 PATCH DTO 和 Endpoint。

## 增加按 Slug 查询

```csharp
[HttpGet("by-slug/{slug}")]
[ProducesResponseType(
    typeof(BlogPost),
    StatusCodes.Status200OK)]
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
```

Route 使用固定 Segment `by-slug`，避免与按数字 ID 查询的 Route 混淆：

```text
GET /api/blogs/12
GET /api/blogs/by-slug/ef-core-guide
```

Unique Index 保证查询最多匹配一篇文章。

## 验证创建

```http
POST /api/blogs
Content-Type: application/json

{
  "slug": "ef-core-guide",
  "title": "EF Core Guide",
  "content": "Article body"
}
```

预期 `201 Created`，Response 中的 Slug 是 `ef-core-guide`。

## 验证查询

```http
GET /api/blogs/by-slug/ef-core-guide
```

预期 `200 OK`。

```http
GET /api/blogs/by-slug/not-exist
```

预期 `404 Not Found`。

## 完成标准

- 缺少或格式错误的 Slug 返回 `400`；
- POST 能保存非空 Slug；
- PUT 能修改 Slug；
- 按 Slug 查询返回 `200` 或 `404`；
- 数据库中的旧文章和新文章都拥有非空 Slug。

## 下一步

此时使用重复 Slug 会触发 Database Unique Violation。如果异常尚未转换，API 可能返回 `500`。下一篇将只解决这一个错误映射问题。

## 参考资料

- [ASP.NET Core model validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation)
- [EF Core querying](https://learn.microsoft.com/ef/core/querying/)

## 主线导航

- 上一步：[为已有表安全添加 Slug](./09-ef-core-add-required-column-with-existing-data.md)
- 下一步：[把唯一冲突转换为 409](./10-aspnet-core-postgresql-unique-conflict-409.md)
