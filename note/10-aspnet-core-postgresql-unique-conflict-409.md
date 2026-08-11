---
title: "ASP.NET Core：把 PostgreSQL 唯一冲突转换为 409"
description: "使用 EF Core、Npgsql 和 ProblemDetails，把数据库 Unique Violation 从 500 转换成明确的 HTTP 409 Conflict。"
tags:
  - ASP.NET Core
  - EF Core
  - PostgreSQL
  - Npgsql
  - ProblemDetails
---

# ASP.NET Core：把 PostgreSQL 唯一冲突转换为 409

数据库 Unique Index 能阻止重复数据，但如果 API 不识别这个错误，客户端通常只能得到模糊的 `500 Internal Server Error`。

以文章 Slug 为例，目标是：

```text
第一次保存相同 Slug → 201 Created
再次保存相同 Slug → 409 Conflict
```

## 数据库必须是最后一道防线

可以在保存前先查询：

```csharp
var exists = await _dbContext.BlogPosts
    .AnyAsync(post => post.Slug == dto.Slug);
```

但两个并发请求可能同时查询到 `false`，然后一起执行 INSERT。只依靠 API 预检查无法保证唯一性。

最终约束必须存在于 PostgreSQL：

```csharp
modelBuilder.Entity<BlogPost>()
    .HasIndex(post => post.Slug)
    .IsUnique();
```

## 识别唯一约束异常

EF Core 保存失败时抛出 `DbUpdateException`，PostgreSQL 的具体错误位于 Inner Exception 中。

Controller 需要：

```csharp
using Microsoft.EntityFrameworkCore;
using Npgsql;
```

可以集中判断：

```csharp
private static bool IsSlugConflict(DbUpdateException exception)
{
    return exception.InnerException is PostgresException postgresException
        && postgresException.SqlState
            == PostgresErrorCodes.UniqueViolation
        && postgresException.ConstraintName
            == "IX_BlogPosts_Slug";
}
```

三项条件分别确认：

```text
PostgresException               → 错误来自 PostgreSQL
UniqueViolation                 → SQLSTATE 是 23505
IX_BlogPosts_Slug               → 冲突来自 Slug Index
```

检查 Constraint Name 很重要。一个表以后可能还有邮箱、外部编号等 Unique Constraint，不能把所有数据库冲突都误报成 Slug 重复。

## 转换为 409 Conflict

在保存位置捕获已知冲突：

```csharp
try
{
    await _dbContext.SaveChangesAsync();
}
catch (DbUpdateException exception)
    when (IsSlugConflict(exception))
{
    return Conflict(new ProblemDetails
    {
        Title = "Slug already exists.",
        Detail = $"The slug '{dto.Slug}' is already in use.",
        Status = StatusCodes.Status409Conflict
    });
}
```

返回结果：

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
```

```json
{
  "title": "Slug already exists.",
  "status": 409,
  "detail": "The slug 'ef-core-guide' is already in use."
}
```

`409` 表示请求格式正确，但当前资源状态与请求发生冲突。它比 `400` 更准确，也比 `500` 更能说明客户端应该更换 Slug。

## 创建和更新都要处理

重复 Slug 不只会发生在 POST。PUT 把一篇文章的 Slug 改成其他文章已使用的值时，也会触发同一 Unique Index。

因此创建和更新路径都要转换这个异常。项目变大后，可以把重复的异常转换移到统一异常处理层；在小型 Controller 中先显式处理，更容易看清执行过程。

## 不要向客户端返回数据库详情

PostgreSQL Exception 可能包含 SQL、Constraint 或内部结构信息。API 应返回稳定的 ProblemDetails，而不是直接把 Exception Message 暴露给客户端。

日志可以保留内部异常，HTTP Response 只返回客户端需要的信息。

## 验证

至少验证三个场景：

| 场景 | 预期结果 |
|---|---|
| 使用新 Slug 创建文章 | `201 Created` |
| 使用重复 Slug 创建文章 | `409 Conflict` |
| PUT 更新为其他文章的 Slug | `409 Conflict` |

冲突后再次查询数据库，确认失败请求没有修改原记录。

## 总结

完整职责链是：

```text
DTO             → 验证 Slug 格式
Database Index  → 最终保证 Slug 唯一
API             → 把 23505 转换成 409
ProblemDetails  → 返回稳定错误结构
```

API 可以提供友好的错误，但数据完整性必须由数据库最终保证。

## 参考资料

- [Npgsql Exceptions](https://www.npgsql.org/doc/diagnostics/exceptions_notices.html)
- [PostgresErrorCodes.UniqueViolation](https://www.npgsql.org/doc/api/Npgsql.PostgresErrorCodes.html)
- [ASP.NET Core ProblemDetails](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api)

## 主线导航

- 上一步：[把 Slug 接入文章 API](./09a-add-slug-to-blog-api.md)
- 下一步：[实现稳定分页](./11-ef-core-stable-pagination.md)
