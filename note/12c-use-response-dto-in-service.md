---
title: "使用 Response DTO 隔离 EF Core Entity"
description: "让 ASP.NET Core Service 返回稳定的 Response DTO，并正确处理 EF Core 查询投影、创建后的生成 ID、被追踪 Entity 更新和 DELETE 结果。"
tags:
  - ASP.NET Core
  - EF Core
  - DTO
  - Service Layer
---

# 使用 Response DTO 隔离 EF Core Entity

完成 Service Layer 后，API 虽然已经不在 Controller 中直接访问 `AppDbContext`，但 GET、POST 和 PUT 仍然把 `BlogPost` Entity 作为 Response 返回。这样会让数据库模型同时承担 API Contract 的职责。

本文增加专用 `BlogPostResponseDto`。Service 负责把 Entity 转换成 DTO，Controller 只负责把 Service 结果包装成 `200`、`201`、`404` 或 `204`。

## 前置状态

项目已经具备：

```text
Controller → BlogPostService → AppDbContext → PostgreSQL
```

Request Body 使用带 DataAnnotations 的 `BlogPostDto`，Service CRUD 仍返回 `BlogPost` Entity 或 `null`。

本文只改变 API 输出模型，不修改数据库 Schema，因此不需要 Migration。

## 创建 Response DTO

新建 `Dtos/BlogPostResponseDto.cs`：

```csharp
namespace BlogApi.Dtos;

public class BlogPostResponseDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

Request DTO 和 Response DTO 的职责不同：

```text
BlogPostDto         → 客户端允许提交什么，以及如何验证
BlogPostResponseDto → API 决定向客户端公开什么
BlogPost            → EF Core 如何表示和持久化数据库记录
```

Response DTO 不需要 `[Required]`，因为它不是用来验证 Request Body 的。

## GET 使用查询投影

分页方法的返回类型改为：

```csharp
Task<PagedResultDto<BlogPostResponseDto>>
```

在 `ToListAsync()` 之前使用 `Select()`：

```csharp
var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(post => new BlogPostResponseDto
    {
        Id = post.Id,
        Slug = post.Slug,
        Title = post.Title,
        Content = post.Content,
        CreatedAtUtc = post.CreatedAtUtc
    })
    .ToListAsync();
```

此时 `items` 的类型是：

```csharp
List<BlogPostResponseDto>
```

因此分页容器也必须使用相同泛型参数：

```csharp
return new PagedResultDto<BlogPostResponseDto>
{
    Items = items,
    Page = page,
    PageSize = pageSize,
    TotalItems = totalItems,
    TotalPages = (int)Math.Ceiling(
        totalItems / (double)pageSize)
};
```

`List<BlogPost>` 不会因为两个类拥有相似属性就自动转换成 `List<BlogPostResponseDto>`。`Select()` 明确描述了每个字段如何映射，EF Core 也能把这个投影翻译进 SQL Query。

按 ID 和 Slug 查询使用相同方式：

```csharp
return await _dbContext.BlogPosts
    .AsNoTracking()
    .Select(post => new BlogPostResponseDto
    {
        Id = post.Id,
        Slug = post.Slug,
        Title = post.Title,
        Content = post.Content,
        CreatedAtUtc = post.CreatedAtUtc
    })
    .FirstOrDefaultAsync(post => post.Id == id);
```

查询可能找不到记录，所以返回类型保留可空标记：

```csharp
Task<BlogPostResponseDto?>
```

## POST 保存后直接映射

创建方法返回保存后的 DTO：

```csharp
public async Task<BlogPostResponseDto> CreatePostAsync(
    BlogPost blogPost)
{
    await _dbContext.BlogPosts.AddAsync(blogPost);
    await _dbContext.SaveChangesAsync();

    return new BlogPostResponseDto
    {
        Id = blogPost.Id,
        Slug = blogPost.Slug,
        Title = blogPost.Title,
        Content = blogPost.Content,
        CreatedAtUtc = blogPost.CreatedAtUtc
    };
}
```

`new BlogPost()` 时整数 ID 是 `0`。`AddAsync()` 让当前 DbContext 以 `Added` 状态追踪这个 Entity；`SaveChangesAsync()` 执行 INSERT，并把 PostgreSQL 生成的真实 ID 写回同一个 `blogPost` 对象。

因此保存成功后可以直接映射，不需要再按照 Slug 查询一次数据库。额外查询不仅浪费一次 Round Trip，`FirstOrDefaultAsync()` 还会引入不必要的可空结果。

Controller 使用返回的 ID 构造新资源地址：

```csharp
var createdPost =
    await _blogPostService.CreatePostAsync(post);

return CreatedAtAction(
    nameof(GetPost),
    new { id = createdPost.Id },
    createdPost);
```

成功响应为 `201 Created`，Response Body 是 `BlogPostResponseDto`，`Location` Header 指向按 ID 查询的 Action。

## PUT 必须先修改被追踪的 Entity

更新时不能先 `Select()` 成 DTO，再修改 DTO：

```csharp
// 错误：得到的是 DTO，不是被追踪的 Entity
var post = await _dbContext.BlogPosts
    .Select(post => new BlogPostResponseDto { ... })
    .FirstOrDefaultAsync(...);
```

修改这个 DTO 不会让 `SaveChangesAsync()` 生成 UPDATE。正确流程是先查询 Entity：

```csharp
public async Task<BlogPostResponseDto?> UpdatePostAsync(
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

    return new BlogPostResponseDto
    {
        Id = post.Id,
        Slug = post.Slug,
        Title = post.Title,
        Content = post.Content,
        CreatedAtUtc = post.CreatedAtUtc
    };
}
```

查询没有使用 `AsNoTracking()`，所以 DbContext 能检测属性变化并执行 UPDATE。保存完成后再把 Entity 映射成 Response DTO。

本文选择让 PUT 返回更新后的资源：

```csharp
[ProducesResponseType(
    typeof(BlogPostResponseDto),
    StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<BlogPostResponseDto>> UpdatePost(
    int id,
    [FromBody] BlogPostDto dto)
{
    var result =
        await _blogPostService.UpdatePostAsync(id, dto);

    if (result is null)
    {
        return NotFound();
    }

    return Ok(result);
}
```

也可以把 PUT 设计成 `204 No Content`，但 204 不能包含 Response Body。若选择 204，就不应声明 `BlogPostResponseDto`，也不应构造一个最终会被 Controller 丢弃的 DTO。

## DELETE 只返回是否删除

DELETE 成功时 Controller 返回 `204 No Content`，不使用被删除 Entity，因此 Service 只需返回 `bool`：

```csharp
public async Task<bool> DeletePostAsync(int id)
{
    var post = await _dbContext.BlogPosts.FindAsync(id);

    if (post is null)
    {
        return false;
    }

    _dbContext.BlogPosts.Remove(post);
    await _dbContext.SaveChangesAsync();

    return true;
}
```

Controller 把 Application Result 转成 HTTP Response：

```csharp
var deleted = await _blogPostService.DeletePostAsync(id);

if (!deleted)
{
    return NotFound();
}

return NoContent();
```

## 验证

先编译：

```bash
dotnet build
```

然后验证四类成功响应：

```text
GET    → 200 + BlogPostResponseDto
POST   → 201 + BlogPostResponseDto + Location Header
PUT    → 200 + BlogPostResponseDto
DELETE → 204，无 Response Body
```

还要验证：

- GET 不存在的 ID 返回 `404`；
- PUT 不存在的 ID 返回 `404`；
- PUT 后重新 GET，数据库中确实是新值；
- 重复 Slug 仍由下一阶段的统一异常处理返回 `409`。

不要只检查 PUT Response 中的 DTO。如果误改的是未追踪 DTO，Response 看起来可能已经变化，但数据库实际上没有执行 UPDATE。必须重新 GET 验证持久化结果。

## 完成状态

```text
Request DTO  → 定义输入和 Validation
Entity       → EF Core Tracking 与数据库持久化
Response DTO → 定义稳定的 API 输出
Controller   → HTTP Status Code 与 Location Header
Service      → CRUD、Entity/DTO Mapping
```

## 参考资料

- [ASP.NET Core Action Return Types](https://learn.microsoft.com/aspnet/core/web-api/action-return-types)
- [EF Core Tracking and No-Tracking Queries](https://learn.microsoft.com/ef/core/querying/tracking)
- [EF Core Generated Values](https://learn.microsoft.com/ef/core/modeling/generated-properties)

## 主线导航

- 上一步：[把完整 CRUD 迁入 Service](./12b-refactor-crud-to-service.md)
- 下一步：[使用 IExceptionHandler 统一处理 Slug 冲突](./13-aspnet-core-iexceptionhandler-postgresql-conflict.md)
- 概念补充：[Request DTO 与 Response DTO](./08a-system-text-json-dto-attributes.md)
