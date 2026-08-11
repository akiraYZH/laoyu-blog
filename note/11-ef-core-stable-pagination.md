---
title: "ASP.NET Core：使用 EF Core 实现稳定分页"
description: "使用 Query DTO、泛型分页结果、IQueryable、CountAsync、Skip 和 Take 构建可验证且顺序稳定的分页 API。"
tags:
  - ASP.NET Core
  - EF Core
  - Pagination
  - IQueryable
---

# ASP.NET Core：使用 EF Core 实现稳定分页

直接对列表查询调用 `ToListAsync()` 会读取所有记录。数据量增长后，数据库、API 内存和网络响应都会承担不必要的压力。

分页接口只读取客户端当前需要的一页：

```http
GET /api/blogs?page=2&pageSize=10
```

## 定义分页参数

```csharp
public class PaginationQueryDto
{
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
```

限制 `PageSize` 可以防止客户端通过一个请求读取过多数据。

这个 DTO 不需要 `<T>`。无论查询文章还是用户，请求参数始终只是 Page 和 PageSize。

## 定义泛型分页结果

```csharp
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
```

`T` 是类型占位符：

```text
PagedResultDto<BlogPost> → Items 是 List<BlogPost>
PagedResultDto<User>     → Items 是 List<User>
```

`new()` 根据左侧的 `List<T>` 推断要创建一个空的 `List<T>`。它等价于 `new List<T>()`。

## 构造分页查询

```csharp
[HttpGet]
public async Task<ActionResult<PagedResultDto<BlogPost>>> GetPosts(
    [FromQuery] PaginationQueryDto pagination)
{
    var query = _dbContext.BlogPosts
        .AsNoTracking()
        .OrderByDescending(post => post.CreatedAtUtc)
        .ThenByDescending(post => post.Id);

    var totalItems = await query.CountAsync();

    var items = await query
        .Skip((pagination.Page - 1) * pagination.PageSize)
        .Take(pagination.PageSize)
        .ToListAsync();

    return Ok(new PagedResultDto<BlogPost>
    {
        Items = items,
        Page = pagination.Page,
        PageSize = pagination.PageSize,
        TotalItems = totalItems,
        TotalPages = (int)Math.Ceiling(
            totalItems / (double)pagination.PageSize)
    });
}
```

## IQueryable 什么时候执行 SQL

下面的代码只是在构造查询：

```csharp
var query = _dbContext.BlogPosts
    .AsNoTracking()
    .OrderByDescending(post => post.CreatedAtUtc);
```

`CountAsync()` 第一次执行 SQL，用于获取总记录数。`ToListAsync()` 第二次执行 SQL，用于获取当前页数据。

分页通常产生两次查询：

```sql
SELECT COUNT(*) FROM "BlogPosts";
```

```sql
SELECT *
FROM "BlogPosts"
ORDER BY "CreatedAtUtc" DESC, "Id" DESC
LIMIT 10 OFFSET 10;
```

## Skip 和 Take

跳过数量的公式是：

```text
(Page - 1) × PageSize
```

每页 10 条时：

```text
第 1 页 → Skip 0
第 2 页 → Skip 10
第 3 页 → Skip 20
```

`Take(PageSize)` 决定当前页最多读取多少条。

## 为什么需要 ThenBy

只按照创建时间排序不一定稳定。两篇文章可能拥有相同时间，数据库可以在不同查询中交换它们的顺序。

```csharp
.OrderByDescending(post => post.CreatedAtUtc)
.ThenByDescending(post => post.Id)
```

使用唯一 ID 作为第二排序键，可以避免翻页时出现重复或遗漏。

## 总页数为什么需要 double

```csharp
TotalPages = (int)Math.Ceiling(
    totalItems / (double)pageSize);
```

整数相除会丢失小数：

```text
25 / 10 = 2
```

转换为 `double` 后：

```text
25 / 10.0 = 2.5
Ceiling(2.5) = 3
```

## 为什么 GET 会返回 415

如果遗漏 `[FromQuery]`：

```csharp
GetPosts(PaginationQueryDto pagination)
```

`[ApiController]` 可能把复杂类型推断为 Request Body。GET 没有 JSON Body 和对应 Content-Type 时，就会返回 `415 Unsupported Media Type`。

明确写出来源：

```csharp
GetPosts([FromQuery] PaginationQueryDto pagination)
```

这样 Page 和 PageSize 才会从 Query String 绑定。

## 是否需要分页 Library

简单 REST 分页通常直接组合 EF Core 的 `Skip`、`Take` 和 `CountAsync`。这不是重新实现数据库分页，而是在使用 EF Core 已有能力并定义自己的 API Contract。

多个列表重复相同逻辑后，可以提取项目内部 Extension。只有动态过滤、排序和字段选择非常复杂时，才需要评估外部查询 Library。

## 验证

```http
GET /api/blogs?page=1&pageSize=10
GET /api/blogs?page=2&pageSize=10
GET /api/blogs?page=0&pageSize=200
```

前两个请求应返回分页数据，最后一个应因为参数超出 `[Range]` 返回 `400 Bad Request`。

## 总结

稳定分页需要同时处理：

```text
Query DTO      → 限制 Page 和 PageSize
IQueryable     → 组合 SQL
CountAsync     → 查询总数
Skip / Take    → 读取当前页
稳定排序       → 避免重复和遗漏
PagedResult<T> → 返回分页元数据
```

## 参考资料

- [EF Core Pagination](https://learn.microsoft.com/ef/core/querying/pagination)
- [EF Core Tracking vs. No-Tracking](https://learn.microsoft.com/ef/core/querying/tracking)
- [ASP.NET Core Model Binding](https://learn.microsoft.com/aspnet/core/mvc/models/model-binding)

## 主线导航

- 上一步：[把唯一冲突转换为 409](./10-aspnet-core-postgresql-unique-conflict-409.md)
- 下一步：[把分页查询移入 Service](./12a-refactor-pagination-to-service.md)
