---
title: "把 EF Core 分页查询从 Controller 移入 Service"
description: "创建 Scoped BlogPostService、注册依赖、注入 Controller，并在不改变 API Response 的情况下移动分页查询。"
tags:
  - ASP.NET Core
  - Service Layer
  - Dependency Injection
  - EF Core
---

# 把 EF Core 分页查询从 Controller 移入 Service

分页已经正常工作后，可以把查询实现从 HTTP Controller 移到 Service。本文只移动 `GetPosts` 这一整个用例，其他 CRUD 暂时保持不变。

重构目标是代码位置改变，但外部 API 行为不变。

## 前置状态

Controller 已经支持：

```http
GET /api/blogs?page=1&pageSize=10
```

并返回：

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalItems": 0,
  "totalPages": 0
}
```

先保存一份重构前的实际 Response，后面用于比较。

## 创建 `BlogPostService`

新建 `Services/BlogPostService.cs`：

```csharp
using BlogApi.Data;
using BlogApi.Dtos;
using BlogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Services;

public class BlogPostService
{
    private readonly AppDbContext _dbContext;

    public BlogPostService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<BlogPost>> GetPostsAsync(
        int page,
        int pageSize)
    {
        var query = _dbContext.BlogPosts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAtUtc)
            .ThenByDescending(post => post.Id);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<BlogPost>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize)
        };
    }
}
```

Service 接收普通 `int`，不知道数据来自 Query String，也不返回 `Ok()`。HTTP Concern 继续留在 Controller。

## 注册 Scoped Service

在 `Program.cs` 中：

```csharp
using BlogApi.Services;

// ...

builder.Services.AddScoped<BlogPostService>();
```

`BlogPostService` 依赖 Scoped `AppDbContext`，因此也使用 Scoped Lifetime。每个 HTTP Request 获得一个 Service，并使用同一 Request Scope 中的 DbContext。

目前没有第二个实现，不需要为了形式先创建 `IBlogPostService`。

## 注入 Controller

```csharp
private readonly AppDbContext _dbContext;
private readonly BlogPostService _blogPostService;

public BlogsController(
    AppDbContext dbContext,
    BlogPostService blogPostService)
{
    _dbContext = dbContext;
    _blogPostService = blogPostService;
}
```

暂时保留 `_dbContext`，因为 GET by ID、POST、PUT 和 DELETE 仍然直接使用它。只有所有用例都迁移后，才能从 Controller Constructor 中移除。

## 简化分页 Action

```csharp
[HttpGet]
public async Task<ActionResult<PagedResultDto<BlogPost>>> GetPosts(
    [FromQuery] PaginationQueryDto pagination)
{
    var result = await _blogPostService.GetPostsAsync(
        pagination.Page,
        pagination.PageSize);

    return Ok(result);
}
```

职责变成：

```text
Controller
读取 Query String 并返回 HTTP 200
        ↓
BlogPostService
构造并执行分页查询
        ↓
AppDbContext
访问 PostgreSQL
```

## Build 与验证

```bash
dotnet build
```

重新发送与重构前完全相同的请求：

```http
GET /api/blogs?page=1&pageSize=2
```

比较：

- HTTP Status；
- `items` 顺序；
- Page、PageSize；
- TotalItems、TotalPages。

这些值应与重构前一致。

## 常见错误

### 忘记注册 Service

运行时会提示无法解析 `BlogPostService`。修复：

```csharp
builder.Services.AddScoped<BlogPostService>();
```

### 一开始就删除 Controller 的 DbContext

其他 CRUD 仍使用 `_dbContext` 时会编译失败。渐进式重构期间同时注入两者是正常的过渡状态。

### Service 返回 `IActionResult`

这会让 Service 依赖 HTTP Layer。Service 应返回 Data 或 Application Result，Controller 再决定 `Ok()`、`NotFound()` 或其他 HTTP Response。

## 完成标准

- `dotnet build` 成功；
- DI 能创建 `BlogPostService`；
- 分页 Response 与重构前一致；
- 分页查询代码只存在于 Service；
- Controller 仍负责 `[FromQuery]` 和 `Ok()`。

## 参考资料

- [ASP.NET Core Dependency Injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [EF Core DbContext lifetime](https://learn.microsoft.com/ef/core/dbcontext-configuration/)

## 主线导航

- 上一步：[实现稳定分页](./11-ef-core-stable-pagination.md)
- 架构补充：[什么时候应该加入 Service Layer](./12-when-to-add-service-layer.md)
