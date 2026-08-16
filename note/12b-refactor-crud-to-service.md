---
title: "把完整 CRUD 从 Controller 迁入 Service"
description: "继续重构 ASP.NET Core 博客 API，把按 ID 查询、按 Slug 查询、创建、更新和删除移入 BlogPostService，并让 Controller 只处理 HTTP。"
tags:
  - ASP.NET Core
  - Service Layer
  - EF Core
  - Dependency Injection
---

# 把完整 CRUD 从 Controller 迁入 Service

上一篇只把分页查询移入了 `BlogPostService`。因此 Controller 仍然同时依赖 `BlogPostService` 和 `AppDbContext`，按 ID 查询、创建、更新和删除也仍然包含 EF Core 代码。

本文完成这次渐进式重构：所有数据库访问都进入 Service，Controller 只负责读取 HTTP 输入并选择 HTTP Response。API 的 Route、Request Body 和成功响应保持不变。

## 前置状态

项目已经具有：

```text
GET /api/blogs              → 已使用 BlogPostService
GET /api/blogs/{id}         → 仍直接使用 AppDbContext
GET /api/blogs/by-slug/...  → 仍直接使用 AppDbContext
POST、PUT、DELETE           → 仍直接使用 AppDbContext
```

`Program.cs` 已经注册：

```csharp
builder.Services.AddScoped<BlogPostService>();
```

本篇不创建 Repository，也不急着创建 `IBlogPostService`。当前只有一个实现，先把职责边界整理清楚。

## 为 Service 加入查询方法

在 `Services/BlogPostService.cs` 中加入两个重载：

```csharp
public async Task<BlogPost?> GetPostAsync(int id)
{
    return await _dbContext.BlogPosts
        .AsNoTracking()
        .FirstOrDefaultAsync(post => post.Id == id);
}

public async Task<BlogPost?> GetPostAsync(string slug)
{
    return await _dbContext.BlogPosts
        .AsNoTracking()
        .FirstOrDefaultAsync(post => post.Slug == slug);
}
```

方法名相同，但参数类型不同，这是 C# 的方法重载。调用者传入 `int` 时按 ID 查询，传入 `string` 时按 Slug 查询。

这两个方法只读取数据，因此使用 `AsNoTracking()`。EF Core 不需要为返回的 Entity 保存修改跟踪信息。

## 加入创建方法

```csharp
public async Task CreatePostAsync(BlogPost blogPost)
{
    await _dbContext.BlogPosts.AddAsync(blogPost);
    await _dbContext.SaveChangesAsync();
}
```

`AddAsync()` 把 Entity 加入当前 DbContext，`SaveChangesAsync()` 才真正执行 `INSERT`。保存后，PostgreSQL 生成的 ID 会回填到传入的 `blogPost.Id`，因此 Controller 仍可使用 `CreatedAtAction()` 返回新资源地址。

## 加入更新方法

```csharp
public async Task<BlogPost?> UpdatePostAsync(
    int id,
    BlogPostDto dto)
{
    var post = await _dbContext.BlogPosts
        .FirstOrDefaultAsync(post => post.Id == id);

    if (post is null)
    {
        return null;
    }

    post.Title = dto.Title;
    post.Slug = dto.Slug;
    post.Content = dto.Content;

    await _dbContext.SaveChangesAsync();

    return post;
}
```

这里没有使用 `AsNoTracking()`，因为 EF Core 必须跟踪 `post`，才能在 `SaveChangesAsync()` 时生成 `UPDATE`。

当前接口使用 PUT，而且 `BlogPostDto` 要求三个字段都有效，所以这里执行完整赋值。若未来需要只修改部分字段，应单独设计 PATCH DTO，而不是用空字符串猜测客户端意图。

不存在对应 ID 时返回 `null`。Service 不返回 `NotFound()`，因为 `404` 属于 HTTP 决策，应由 Controller 负责。

## 加入删除方法

```csharp
public async Task<BlogPost?> DeletePostAsync(int id)
{
    var post = await _dbContext.BlogPosts.FindAsync(id);

    if (post is null)
    {
        return null;
    }

    _dbContext.BlogPosts.Remove(post);
    await _dbContext.SaveChangesAsync();

    return post;
}
```

返回被删除的 Entity 不是为了再次输出它，而是让 Controller 区分“删除成功”和“资源不存在”。Controller 成功时仍返回 `204 No Content`。

## Controller 只保留 Service

所有用例迁移完成后，`BlogsController` 不再需要 `AppDbContext`：

```csharp
private readonly BlogPostService _blogPostService;

public BlogsController(BlogPostService blogPostService)
{
    _blogPostService = blogPostService;
}
```

同时删除 Controller 中不再使用的：

```csharp
using BlogApi.Data;
using Microsoft.EntityFrameworkCore;
```

各 Action 改为调用 Service。例如按 ID 查询：

```csharp
var post = await _blogPostService.GetPostAsync(id);

if (post is null)
{
    return NotFound();
}

return Ok(post);
```

更新 Action 的关键部分是：

```csharp
var result = await _blogPostService.UpdatePostAsync(id, dto);

if (result is null)
{
    return NotFound();
}

return Ok(result);
```

必须返回 `result`，因为它是数据库更新后的 Entity。返回请求 DTO 会缺少 `Id`、`CreatedAtUtc` 等服务器字段。

删除 Action 使用同样的空值约定：

```csharp
var post = await _blogPostService.DeletePostAsync(id);

if (post is null)
{
    return NotFound();
}

return NoContent();
```

## 暂时保留唯一冲突 catch

移动 `SaveChangesAsync()` 不会吞掉异常。Service 中发生的 `DbUpdateException` 会沿着 `await` 返回 Controller。

因此，若上一阶段已经在 POST 和 PUT 中处理 Slug 唯一冲突，应暂时保留原有 catch，只把 try 内部的数据库操作替换成 Service 调用：

```csharp
try
{
    await _blogPostService.CreatePostAsync(post);
}
catch (DbUpdateException exception)
    when (IsSlugConflict(exception))
{
    return Conflict(new ProblemDetails
    {
        Title = "Slug already exists.",
        Detail = $"The slug '{post.Slug}' is already in use.",
        Status = StatusCodes.Status409Conflict
    });
}
```

下一篇会把这两段重复 catch 移入 `IExceptionHandler`。在那之前不要同时删除它们，否则重复 Slug 会暂时退化为 500。

## 验证完整 CRUD

先编译：

```bash
dotnet build
```

然后依次验证：

```http
POST   /api/blogs
GET    /api/blogs/{id}
GET    /api/blogs/by-slug/{slug}
PUT    /api/blogs/{id}
DELETE /api/blogs/{id}
```

至少确认：

- 创建返回 `201`，并且 `Location` 中包含新 ID；
- 按 ID 和 Slug 都能取得同一篇文章；
- 更新返回数据库中的完整 Entity；
- 不存在的 ID 返回 `404`；
- 删除成功返回 `204`，再次查询返回 `404`；
- 重复 Slug 仍返回 `409`。

## 完成状态

```text
Controller → HTTP Binding、Status Code、Response
Service    → 完整文章用例与 EF Core 操作
DbContext  → 数据库 Session 与 Change Tracking
PostgreSQL → 数据持久化和唯一约束
```

Controller 中不再出现 `_dbContext`，但它仍通过依赖注入间接使用 Scoped DbContext：Controller → Scoped Service → Scoped DbContext。

## 参考资料

- [ASP.NET Core Dependency Injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [EF Core Tracking and No-Tracking Queries](https://learn.microsoft.com/ef/core/querying/tracking)
- [EF Core Saving Data](https://learn.microsoft.com/ef/core/saving/)

## 主线导航

- 上一步：[把分页查询移入 Service](./12a-refactor-pagination-to-service.md)
- 下一步：[使用 Response DTO 隔离 EF Core Entity](./12c-use-response-dto-in-service.md)
- 架构补充：[什么时候应该加入 Service Layer](./12-when-to-add-service-layer.md)
