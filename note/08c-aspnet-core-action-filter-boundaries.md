---
title: "ASP.NET Core ActionFilter：适用场景与职责边界"
description: "理解 ActionFilter 在 Controller Action 前后的执行位置，并区分 DTO Validation、Middleware、Authorization 和全局异常处理。"
tags:
  - ASP.NET Core
  - ActionFilter
  - Controller
  - Validation
---

# ASP.NET Core ActionFilter：适用场景与职责边界

`ActionFilter` 能读取 Action 参数，也能在 Action 执行前后运行代码，但“能够读取”不代表“应该负责所有验证和错误处理”。它最适合处理多个 Controller Action 共有的横切逻辑。

本文只解释 ActionFilter 的职责边界，不向博客 API 增加新的 Filter。

## ActionFilter 在请求流程中的位置

Controller API 的相关流程可以简化为：

```text
HTTP Request
    ↓
Model Binding：生成 Route 参数和 DTO
    ↓
DTO Validation
    ↓
ActionFilter：执行前
    ↓
Controller Action
    ↓
ActionFilter：执行后
    ↓
HTTP Response
```

同步 Filter 实现 `IActionFilter`：

```csharp
public sealed class ActionLogFilter : IActionFilter
{
    public void OnActionExecuting(
        ActionExecutingContext context)
    {
        // Action 执行前
    }

    public void OnActionExecuted(
        ActionExecutedContext context)
    {
        // Action 执行后
    }
}
```

异步 Filter 实现 `IAsyncActionFilter`：

```csharp
public async Task OnActionExecutionAsync(
    ActionExecutingContext context,
    ActionExecutionDelegate next)
{
    // before

    var executedContext = await next();

    // after
}
```

`await next()` 执行后续 Pipeline，也就是当前 Controller Action。它之前的代码是 before hook，之后的代码是 after hook。

## `ActionArguments` 与 DTO 不重合

假设 Action 是：

```csharp
public IActionResult UpdatePost(
    int id,
    BlogPostDto dto)
```

Model Binding 完成后，Filter 可以通过：

```csharp
context.ActionArguments
```

看到类似的字典：

```text
"id"  → 12
"dto" → BlogPostDto object
```

DTO 描述一个参数的数据结构；`ActionArguments` 是当前 Action 所有实参的运行时字典。Filter 可以观察 DTO，但不能替代 DTO 提供的强类型 Contract 和 DataAnnotations Validation。

## 适合 ActionFilter 的场景

典型用途包括：

- 记录某组 Action 的开始、结束和参数摘要；
- 统计 Action 执行时间；
- 写入审计信息；
- 在多个 Action 上执行同一项、与 MVC Action 直接相关的检查；
- 统一观察或调整某组 Action 的执行结果。

例如执行时间统计：

```csharp
public sealed class ExecutionTimeFilter : IAsyncActionFilter
{
    private readonly ILogger<ExecutionTimeFilter> _logger;

    public ExecutionTimeFilter(
        ILogger<ExecutionTimeFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        await next();

        stopwatch.Stop();

        _logger.LogInformation(
            "{Action} took {Elapsed} ms",
            context.ActionDescriptor.DisplayName,
            stopwatch.ElapsedMilliseconds);
    }
}
```

## 不适合放进 ActionFilter 的逻辑

| 需求 | 更合适的位置 |
|---|---|
| Title 必填、长度限制 | DTO Validation |
| `id` 数值范围 | Route、参数 Attribute 或 Action |
| 文章是否存在 | Service 或 Controller |
| Slug 是否唯一 | PostgreSQL Unique Index |
| 当前用户能否修改文章 | Authorization |
| 整个 API 的异常响应 | `IExceptionHandler` |
| 所有 HTTP 请求的通用逻辑 | Middleware |

ActionFilter 的 after hook 能观察 Action 异常，但数据库异常统一转换并不是它的首选职责。异常应继续冒泡到全局异常处理中间件。

## ActionFilter 与 Middleware

```text
Middleware
    → 可以覆盖所有 HTTP Request

ActionFilter
    → 只围绕 MVC Controller Action
    → 能直接读取 ActionArguments、ActionDescriptor 和 Action Result
```

如果逻辑需要覆盖静态文件、未匹配路由和其他 Endpoint，优先考虑 Middleware。如果逻辑只与 Controller Action 的参数和结果有关，ActionFilter 更贴近问题。

## 总结

ActionFilter 是 Controller Action 前后的 Hook，而不是通用业务层或验证层。判断是否使用它时，不要只问“能不能读取参数”，而要问“这是不是多个 Action 共有、并且与 Action 执行时机直接相关的逻辑”。

## 参考资料

- [ASP.NET Core Filters](https://learn.microsoft.com/aspnet/core/mvc/controllers/filters)
- [ASP.NET Core Model Binding](https://learn.microsoft.com/aspnet/core/mvc/models/model-binding)

