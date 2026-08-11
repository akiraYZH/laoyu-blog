---
title: "使用 ASP.NET Core 与 EF Core 实现 REST CRUD API"
description: "从 DTO 到 PostgreSQL，理解 GET、POST、PUT、DELETE、Change Tracking、SaveChangesAsync 与常用 HTTP 状态码。"
tags:
  - .NET 10
  - ASP.NET Core
  - EF Core
  - PostgreSQL
  - REST API
---

# 使用 ASP.NET Core 与 EF Core 实现 REST CRUD API

数据库连接和 Migration 完成后，下一步是让 Controller 真正读写 PostgreSQL。CRUD 不只是写四个方法，更重要的是理解每一步由谁负责：

```text
HTTP Request
    ↓
DTO：定义客户端可以提交的数据
    ↓
Controller：处理请求和返回状态码
    ↓
DbContext：跟踪实体并生成 SQL
    ↓
PostgreSQL：持久化数据
```

本文以博客文章 API 为例，重点解释执行边界，而不是堆叠项目结构。

## Entity 与 DTO 不承担相同职责

数据库实体包含数据库管理的字段：

```csharp
public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

创建请求只开放客户端允许填写的字段：

```csharp
public class BlogPostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
```

客户端不应该决定自增 `Id` 和服务器创建时间，因此 Controller 接收 DTO，再把它转换成 Entity。

## GET：只读查询使用 `AsNoTracking`

查询所有文章：

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<BlogPost>>> GetPosts()
{
    var posts = await _dbContext.BlogPosts
        .AsNoTracking()
        .OrderByDescending(post => post.CreatedAtUtc)
        .ToListAsync();

    return Ok(posts);
}
```

`OrderByDescending` 只是在构造查询，`ToListAsync()` 才真正执行 SQL。

`AsNoTracking()` 表示结果只用于读取。EF Core 不需要记录每个实体的原始状态，可以减少跟踪开销。

查询单篇文章时，找不到应返回 `404`：

```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<BlogPost>> GetPost(int id)
{
    var post = await _dbContext.BlogPosts
        .AsNoTracking()
        .FirstOrDefaultAsync(post => post.Id == id);

    return post is null ? NotFound() : Ok(post);
}
```

## POST：`Add` 不等于写入数据库

```csharp
[HttpPost]
public async Task<ActionResult<BlogPost>> CreatePost(
    [FromBody] BlogPostDto dto)
{
    var post = new BlogPost
    {
        Title = dto.Title,
        Content = dto.Content
    };

    _dbContext.BlogPosts.Add(post);
    await _dbContext.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetPost),
        new { id = post.Id },
        post);
}
```

两行代码的职责不同：

```text
Add(post)              → 将实体标记为 Added
SaveChangesAsync()     → 执行 INSERT 并取得数据库生成的 Id
```

如果遗漏 `SaveChangesAsync()`，接口仍然可以手动返回 `201`，但数据并未进入 PostgreSQL，`post.Id` 也会保持默认值 `0`。

### `CreatedAtAction` 的三个参数

```csharp
CreatedAtAction(
    nameof(GetPost),
    new { id = post.Id },
    post);
```

- `nameof(GetPost)`：使用哪个 Action 的路由生成资源地址。
- `new { id = post.Id }`：填充该路由需要的 `{id}`。
- `post`：放入 HTTP response body 的对象。

成功响应包括：

```http
HTTP/1.1 201 Created
Location: /api/blogs/7
```

`CreatedAtAction` 不会保存数据库，也不会调用 `GetPost()`；它只负责构造 HTTP 响应。

## PUT：更新需要 Change Tracking

```csharp
[HttpPut("{id:int}")]
public async Task<ActionResult<BlogPost>> UpdatePost(
    int id,
    [FromBody] BlogPostDto dto)
{
    var post = await _dbContext.BlogPosts
        .FirstOrDefaultAsync(post => post.Id == id);

    if (post is null)
    {
        return NotFound();
    }

    post.Title = dto.Title;
    post.Content = dto.Content;

    await _dbContext.SaveChangesAsync();

    return Ok(post);
}
```

这里不要使用 `AsNoTracking()`。EF Core 需要跟踪查询结果，才能发现 `Title` 和 `Content` 已改变，并在保存时生成 `UPDATE`。

如果接口只更新请求中出现的字段，它实际上更接近 `PATCH`。严格的 `PUT` 通常要求客户端发送资源的完整可更新表示。

## DELETE：`Remove` 之后仍要保存

删除请求只需要路由中的 ID，不需要 DTO 或 request body：

```csharp
[HttpDelete("{id:int}")]
public async Task<IActionResult> DeletePost(int id)
{
    var post = await _dbContext.BlogPosts.FindAsync(id);

    if (post is null)
    {
        return NotFound();
    }

    _dbContext.BlogPosts.Remove(post);
    await _dbContext.SaveChangesAsync();

    return NoContent();
}
```

执行过程是：

```text
FindAsync(id)          → 按主键查询并跟踪实体
Remove(post)           → 将实体标记为 Deleted
SaveChangesAsync()     → 真正执行 DELETE
NoContent()            → 返回 204，不包含 response body
```

## HTTP 状态码应该表达结果

| 场景 | 状态码 |
|---|---:|
| 查询成功 | `200 OK` |
| 创建成功 | `201 Created` |
| 更新成功并返回实体 | `200 OK` |
| 删除成功且不返回内容 | `204 No Content` |
| 指定 ID 不存在 | `404 Not Found` |
| 路径存在但 HTTP 方法不受支持 | `405 Method Not Allowed` |

`405` 通常意味着路由存在，但没有匹配当前 HTTP Verb 的 Action。例如只有 `[HttpGet("{id}")]`，却向同一路径发送 PUT。

## 使用 `.http` 文件验证完整流程

```http
@HostAddress = http://localhost:8080

POST {{HostAddress}}/api/blogs
Content-Type: application/json

{
  "title": "第一篇文章",
  "content": "正文"
}

###

GET {{HostAddress}}/api/blogs/1
Accept: application/json

###

PUT {{HostAddress}}/api/blogs/1
Content-Type: application/json

{
  "title": "更新后的标题",
  "content": "更新后的正文"
}

###

DELETE {{HostAddress}}/api/blogs/1

###
```

验证时不要只看第一个响应。创建后要 GET，更新后要再次 GET，删除后同一 ID 应返回 `404`。这样才能证明变化确实写入数据库，而不是只返回了一个看起来正确的 HTTP 状态码。

## 总结

EF Core CRUD 的核心边界可以压缩成四句话：

```text
只读查询使用 AsNoTracking
Add 和 Remove 只改变跟踪状态
SaveChangesAsync 才真正执行 INSERT、UPDATE、DELETE
HTTP 状态码只描述结果，不能代替数据库验证
```

理解这些边界后，Controller 代码就不再是一组需要背诵的方法，而是一条可以逐步验证的数据处理流程。

## 参考资料

- [ASP.NET Core Web API Controller](https://learn.microsoft.com/aspnet/core/web-api/)
- [EF Core 保存数据](https://learn.microsoft.com/ef/core/saving/)

## 主线导航

- 上一步：[封装 Database Update 与回滚](./05a-safe-ef-core-database-update-script.md)
- 下一步：[加入 Request Validation](./08-aspnet-core-dto-common-attributes.md)
