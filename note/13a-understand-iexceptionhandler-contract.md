---
title: "读懂 IExceptionHandler：ValueTask、CancellationToken 与 ProblemDetails"
description: "拆解 TryHandleAsync 的参数、返回值和 Response 写入过程，理解 ASP.NET Core 如何判断异常是否已经处理。"
tags:
  - ASP.NET Core
  - IExceptionHandler
  - ValueTask
  - CancellationToken
  - ProblemDetails
---

# 读懂 IExceptionHandler：ValueTask、CancellationToken 与 ProblemDetails

实现 `IExceptionHandler` 时，核心方法是：

```csharp
ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken);
```

这不是普通的业务方法，而是 ASP.NET Core 异常处理中间件调用的接口契约。理解三个参数和 `bool` 返回值后，Handler 的控制流就会变得清晰。

## 调用者是谁

Controller 不会手动调用 `TryHandleAsync()`。异常处理中间件捕获未处理异常后，会按注册顺序调用 Handler，内部逻辑可以简化为：

```csharp
foreach (var handler in handlers)
{
    var handled = await handler.TryHandleAsync(
        httpContext,
        exception,
        httpContext.RequestAborted);

    if (handled)
    {
        break;
    }
}
```

## `HttpContext`

`HttpContext` 表示当前 HTTP Request 和 Response。Handler 使用它设置状态码并写入响应：

```csharp
httpContext.Response.StatusCode =
    StatusCodes.Status409Conflict;
```

这里修改的是真实 HTTP Status Code，而 `ProblemDetails.Status` 只是 JSON Body 中的字段。通常两处保持一致。

## `Exception`

```csharp
Exception exception
```

是 Pipeline 中未被处理的异常。它可能是 `DbUpdateException`，也可能是其他错误。具体 Handler 应只处理自己认识的异常：

```csharp
if (!IsSlugConflict(exception))
{
    return false;
}
```

## `CancellationToken`

异常处理中间件通常把当前请求的：

```csharp
httpContext.RequestAborted
```

传入 `cancellationToken`。客户端取消请求、断开连接或网络中止时，Kestrel 会触发这个 Token。

把它继续传给异步 I/O：

```csharp
await httpContext.Response.WriteAsJsonAsync(
    problemDetails,
    cancellationToken);
```

这样客户端已经离开时，服务器不必继续写入无用的 Response。

CancellationToken 是协作式通知，不会强制杀死代码。操作必须主动检查它，或把它传给支持取消的异步方法。

## 为什么返回 `ValueTask<bool>`

`ValueTask<bool>` 表示一个可能立即完成、最终得到 `bool` 的操作。Handler 经常可以快速判断异常不属于自己：

```csharp
return false;
```

在 `async ValueTask<bool>` 方法中可以直接返回布尔值；如果方法没有 `async`，可以使用：

```csharp
return ValueTask.FromResult(false);
```

普通应用方法通常继续使用 `Task<T>`。这里使用 `ValueTask<bool>` 的直接原因是 `IExceptionHandler` 接口如此规定。

## 最后一个 bool 的影响

```text
return true
    → 当前异常已经处理
    → 停止调用后续 Handler

return false
    → 当前 Handler 不负责
    → 继续调用下一个 Handler 或后备处理
```

`true` 不会自动设置 HTTP 200 或 409。Status Code 由：

```csharp
httpContext.Response.StatusCode
```

决定。

不要在写完 Response 后返回 `false`。这会告诉 Framework 继续处理一个已经开始写响应的请求。

## `ProblemDetails`

`ProblemDetails` 是 ASP.NET Core 提供的标准 API 错误 DTO，常用字段包括：

| 字段 | 作用 |
|---|---|
| `Type` | 标识错误类别的 URI |
| `Title` | 简短、稳定的错误标题 |
| `Status` | JSON 中对应的 HTTP 状态码 |
| `Detail` | 当前错误的具体说明 |
| `Instance` | 发生错误的请求路径或资源标识 |

它是给客户端看的稳定 Contract，不是服务器内部 Exception。不要把 Stack Trace、SQL 或真实文件路径直接放进 `Detail`。

## `WriteAsJsonAsync`

```csharp
await httpContext.Response.WriteAsJsonAsync(
    problemDetails,
    cancellationToken);
```

这行代码：

1. 使用 JSON Serializer 把 `ProblemDetails` 转成 JSON；
2. 把 JSON 写入当前 Response Body；
3. 异步等待写入完成；
4. 请求取消时停止写入。

它不会替代前面的：

```csharp
httpContext.Response.StatusCode = 409;
```

前者写 Body，后者设置 HTTP 协议状态。

## 总结

```text
HttpContext        → 当前 Request 和 Response
Exception          → 尚未处理的错误
CancellationToken  → 当前请求是否已经取消
ProblemDetails     → 返回给客户端的错误 DTO
WriteAsJsonAsync   → 写入 JSON Response Body
true / false       → 是否继续寻找其他 Handler
```

## 参考资料

- [IExceptionHandler API](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.diagnostics.iexceptionhandler)
- [ASP.NET Core ProblemDetails](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api)
- [Cancellation in Managed Threads](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads)

