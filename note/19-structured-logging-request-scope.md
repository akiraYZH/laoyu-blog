---
title: "使用日志 Scope 关联用户、请求与文章操作"
description: "通过 ASP.NET Core Middleware、ILogger 与 BeginScope，让文章写操作日志包含 TraceId、UserId 和 PostId。"
tags:
  - ASP.NET Core
  - Structured Logging
  - Middleware
  - ILogger
---

# 使用日志 Scope 关联用户、请求与文章操作

`Blog post published` 只能说明某件事发生了，却无法回答是哪篇文章、谁操作、属于哪次 HTTP 请求。本文为写操作加入结构化字段：

```text
PostId  → 操作哪个资源
UserId  → 谁执行操作
TraceId → 属于哪次请求
Action  → created、updated、published、unpublished 或 deleted
```

## 为什么不用 ActionFilter 记录业务结果

ActionFilter 适合 Controller Action 前后的通用 Hook，例如执行时间。它无法自然判断发布调用是首次改变状态、重复请求、404，还是数据库保存失败。

`ILogger` 才是日志抽象；即使创建 Filter，Filter 本身仍需注入 `ILogger<Filter>`。因此本项目采用：

```text
Middleware → 建立请求级 UserId、TraceId Scope
Service    → 在业务操作真正完成后记录 PostId 和 Action
ILogger    → 把结构化日志交给 Console Provider
```

## 建立请求日志 Scope

创建 `Middleware/RequestLogContextMiddleware.cs`：

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace BlogApi.Middleware;

public sealed class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLogContextMiddleware> _logger;

    public RequestLogContextMiddleware(
        RequestDelegate next,
        ILogger<RequestLogContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value
            ?? "anonymous";

        using (_logger.BeginScope(
            "TraceId: {TraceId} UserId: {UserId}",
            context.TraceIdentifier,
            userId))
        {
            await _next(context);
        }
    }
}
```

`BeginScope()` 不会立即写日志，而是给 Scope 内的日志附加公共上下文。`await _next(context)` 执行后续 Middleware、Controller 和 Service。

`using` 会在请求完成或抛出异常时调用 Scope 的 `Dispose()`，可近似理解为：

```csharp
var scope = _logger.BeginScope(...);

try
{
    await _next(context);
}
finally
{
    scope?.Dispose();
}
```

清理 Scope 避免当前请求的信息影响后续请求。

## Middleware 必须放在认证之后

```csharp
app.UseAuthentication();
app.UseMiddleware<RequestLogContextMiddleware>();
app.UseAuthorization();
```

`UseAuthentication()` 先验证 JWT 并建立 `HttpContext.User`。日志 Middleware 随后才能从 `sub` Claim 取得 UserId。匿名请求使用稳定的 `anonymous`，不要把 Token、密码或完整请求 Body 写入日志。

## 在 Service 记录业务事件

通过构造函数注入分类 Logger：

```csharp
private readonly ILogger<BlogPostService> _logger;

public BlogPostService(
    AppDbContext dbContext,
    CategoryService categoryService,
    ILogger<BlogPostService> logger)
{
    _dbContext = dbContext;
    _categoryService = categoryService;
    _logger = logger;
}
```

数据库保存成功后记录：

```csharp
_logger.LogInformation(
    "Blog post {PostId} created.",
    blogPost.Id);
```

找不到写操作目标时记录 Warning：

```csharp
_logger.LogWarning(
    "Blog post {PostId} was not found for deletion.",
    id);
```

不要为了写日志捕获后立刻重新抛出每个数据库异常，否则全局异常处理器和 Framework 可能重复记录同一个错误。

## 显示 Console Scope

在 `appsettings.json` 开启 Scope：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "Console": {
      "FormatterOptions": {
        "IncludeScopes": true
      }
    }
  }
}
```

当前项目没有把日志写入文件或数据库。`WebApplication.CreateBuilder` 注册的 Console Provider 把日志写到标准输出。

## 查看日志

直接运行时查看当前终端。使用 Compose 时：

```bash
docker compose --env-file .env.development logs --tail=100 api
docker compose --env-file .env.development logs -f api
```

执行一次发布后可看到类似：

```text
TraceId: 0HN... UserId: user-id
Blog post 12 published.
```

按 `Ctrl+C` 只停止跟踪日志，不会停止 Container。

当前 Console Log 适合本地排错，不是永久审计记录。生产环境可以继续输出 stdout，再由 CloudWatch 等集中日志服务收集。

## 验证

```bash
dotnet build
dotnet test
```

然后分别创建、更新、发布、取消发布和删除一篇测试文章，确认日志中的 PostId 正确，同一次请求带有相同 UserId 与 TraceId。不要使用真实生产内容做破坏性测试。

## 总结

日志职责已经分离：Middleware 提供请求上下文，Service 在真实状态变化后记录业务事件，Logger Provider 决定输出位置。日志能够回答“谁在什么请求中对哪篇文章做了什么”。

## 参考资料

- [Logging in .NET and ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/logging/)
- [Write custom ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write)

## 主线导航

- 上一步：[为 API 和 PostgreSQL 添加 Health Check](./18-add-api-database-health-check.md)
- 下一步：[返回系列目录](./README.md)
