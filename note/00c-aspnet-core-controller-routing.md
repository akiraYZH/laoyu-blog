---
title: "ASP.NET Core Controller 的 Attribute Routing 如何决定 URL"
description: "理解 ApiController、Route、HttpGet、Route Constraint、ActionResult 与 404/405 的关系。"
tags:
  - ASP.NET Core
  - Controller
  - Routing
  - HTTP
---

# ASP.NET Core Controller 的 Attribute Routing 如何决定 URL

访问 API 时最常见的困惑是：应用已经启动，为什么某个 URL 仍然返回 `404` 或 `405`？答案通常在 Controller 的 Attribute Routing 中。

## Controller Route

```csharp
[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
}
```

`[controller]` 会使用类名去掉 `Controller` 后缀：

```text
BlogsController → blogs
```

因此 Controller Route 是：

```text
/api/blogs
```

## Action Route

```csharp
[HttpGet]
public IActionResult GetPosts()
```

没有额外 Route Template，因此匹配：

```http
GET /api/blogs
```

带 ID 的 Action：

```csharp
[HttpGet("{id:int}")]
public IActionResult GetPost(int id)
```

匹配：

```http
GET /api/blogs/12
```

`int` 是 Route Constraint。`/api/blogs/abc` 不会匹配这个 Action。

## 路由组合

最终 URL 来自两部分组合：

```text
Controller: api/[controller]
Action:     {id:int}
Result:     api/blogs/{id:int}
```

方法名称 `GetPost` 本身不会自动成为 URL。

## 为什么根路径返回 404

如果只定义了：

```text
/api/blogs
/api/blogs/{id}
```

那么访问 `/` 返回 `404` 是正确行为。Server 已启动不代表每个 URL 都存在。

## 404 与 405

```text
404 Not Found          → 没有匹配的 Endpoint，或资源不存在
405 Method Not Allowed → URL Route 存在，但 HTTP Method 不受支持
```

例如只定义：

```csharp
[HttpGet("{id:int}")]
```

却发送：

```http
PUT /api/blogs/1
```

就可能得到 `405`。

## `ActionResult<T>`

```csharp
public ActionResult<BlogPost> GetPost(int id)
```

它允许 Action 返回数据，也允许返回 HTTP Result：

```csharp
return Ok(post);
return NotFound();
return BadRequest();
```

常用映射：

| 方法 | 状态码 |
|---|---:|
| `Ok(value)` | 200 |
| `CreatedAtAction(...)` | 201 |
| `NoContent()` | 204 |
| `BadRequest()` | 400 |
| `NotFound()` | 404 |
| `Conflict()` | 409 |

## 总结

排查路由时不要猜 URL，而是依次读取：

```text
Controller 的 [Route]
        ↓
Action 的 [HttpGet]/[HttpPost]/...
        ↓
Route Parameter 和 Constraint
        ↓
实际 HTTP Method 与 URL
```

## 参考资料

- [Routing to controller actions](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing)
- [Controller action return types](https://learn.microsoft.com/aspnet/core/web-api/action-return-types)

