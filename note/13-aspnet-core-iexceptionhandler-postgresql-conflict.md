---
title: "使用 IExceptionHandler 统一处理 PostgreSQL Slug 冲突"
description: "把 Controller 中重复的 DbUpdateException catch 移入全局异常处理管道，并统一返回 HTTP 409 ProblemDetails。"
tags:
  - ASP.NET Core
  - IExceptionHandler
  - EF Core
  - PostgreSQL
  - ProblemDetails
---

# 使用 IExceptionHandler 统一处理 PostgreSQL Slug 冲突

数据库 Unique Index 已经能够阻止重复 Slug，Controller 也能通过 `try/catch` 把 `DbUpdateException` 转换为 `409 Conflict`。当 Create 和 Update 都出现相同 catch 时，可以把 HTTP 错误转换集中到 ASP.NET Core 的异常处理管道。

本次重构不改变数据库约束，也不改变成功响应；目标只是删除 Controller 中重复的异常转换代码。

## 前置状态

项目已经具有：

```text
BlogPosts.Slug NOT NULL
IX_BlogPosts_Slug UNIQUE
POST 和 PUT 会调用 SaveChangesAsync()
完整 CRUD 已经由 BlogPostService 执行
重复 Slug 当前由 Controller catch 转换成 409
```

## 创建具体职责的 Handler

新建 `Exceptions/SlugConflictExceptionHandler.cs`：

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlogApi.Exceptions;

public sealed class SlugConflictExceptionHandler
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!IsSlugConflict(exception))
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Slug already exists.",
            Detail = "A blog post with this slug already exists.",
            Status = StatusCodes.Status409Conflict
        };

        httpContext.Response.StatusCode =
            StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static bool IsSlugConflict(
        Exception exception)
    {
        return exception
                is DbUpdateException dbUpdateException
            && dbUpdateException.InnerException
                is PostgresException postgresException
            && postgresException.SqlState
                == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName
                == "IX_BlogPosts_Slug";
    }
}
```

Handler 名称使用具体职责，而不是模糊的 `GlobalExceptionHandler`。它会在全局 Pipeline 中运行，但只处理 Slug Constraint 冲突；其他异常返回 `false`，继续交给后续 Handler 或后备错误处理。

## 注册服务

在 `Program.cs` 引入：

```csharp
using BlogApi.Exceptions;
```

注册 ProblemDetails 和 Handler：

```csharp
builder.Services.AddProblemDetails();

builder.Services
    .AddExceptionHandler<SlugConflictExceptionHandler>();
```

`AddExceptionHandler<T>()` 让 Dependency Injection 知道如何创建 Handler。它本身不会把 Middleware 加入请求 Pipeline。

## 把异常处理中间件加入 Pipeline

在 `builder.Build()` 之后、其他应用 Middleware 之前加入：

```csharp
var app = builder.Build();

app.UseExceptionHandler();
```

这一步不能省略。只有注册而没有 `UseExceptionHandler()` 时，未处理异常不会进入这套异常处理 Pipeline。

## 删除 Controller 中的重复 catch

完整 CRUD 已经进入 `BlogPostService`，因此 Controller 中的 Create 和 Update 只保留 Service 调用：

```csharp
await _blogPostService.CreatePostAsync(post);

var result = await _blogPostService.UpdatePostAsync(id, dto);
```

删除：

```csharp
try
{
    await _dbContext.SaveChangesAsync();
}
catch (DbUpdateException exception)
    when (IsSlugConflict(exception))
{
    return Conflict(...);
}
```

也删除 Controller 中的 `IsSlugConflict()` Helper 和不再使用的 `using Npgsql;`。

如果 Controller 仍然 catch 这个异常，异常已经在本地被处理，Handler 永远不会收到它。

## 运行时调用链

Handler 不由 Controller 手动调用：

```text
POST /api/blogs
    ↓
Controller 或 Service 调用 SaveChangesAsync()
    ↓
PostgreSQL 抛出 UniqueViolation
    ↓
EF Core 抛出 DbUpdateException
    ↓
异常继续向外冒泡
    ↓
UseExceptionHandler 捕获
    ↓
调用 SlugConflictExceptionHandler.TryHandleAsync()
    ↓
返回 409 ProblemDetails
```

## 验证

先编译：

```bash
dotnet build
```

连续发送两次相同请求：

```http
POST http://localhost:8080/api/blogs
Content-Type: application/json

{
  "slug": "exception-handler-test",
  "title": "Exception Handler Test",
  "content": "Testing centralized error handling."
}
```

第一次应返回 `201 Created`；第二次应返回：

```http
409 Conflict
```

```json
{
  "title": "Slug already exists.",
  "status": 409,
  "detail": "A blog post with this slug already exists."
}
```

不要只看状态码。稳定的 `title` 和 `detail` 能帮助确认响应来自 Handler，而不是遗留的 Controller catch。

## 常见错误

### 只注册，没有使用 Middleware

```csharp
builder.Services
    .AddExceptionHandler<SlugConflictExceptionHandler>();
```

还必须加入：

```csharp
app.UseExceptionHandler();
```

### 写完 Response 却返回 false

Handler 写完 JSON 后应返回 `true`。`false` 表示没有处理，Framework 会继续尝试其他 Handler，可能再次写 Response。

### 把所有数据库异常都转成 409

必须同时检查 PostgreSQL SQLSTATE 和 Constraint Name。未知的 `DbUpdateException` 不应伪装成 Slug 冲突。

## 完成状态

```text
Database Unique Index → 保证 Slug 唯一
SlugConflictExceptionHandler → 把已知冲突转换为 409
ProblemDetails → 提供稳定的错误 JSON
Controller → 不再包含重复的数据库异常 catch
```

## 参考资料

- [ASP.NET Core Error Handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
- [ASP.NET Core Web API Error Handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api)
- [Npgsql Exception Diagnostics](https://www.npgsql.org/doc/diagnostics/exceptions_notices.html)

## 主线导航

- 上一步：[使用 Response DTO 隔离 EF Core Entity](./12c-use-response-dto-in-service.md)
