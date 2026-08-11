---
title: "ASP.NET Core 参数从哪里来：Body、Route、Query 与 Header"
description: "理解 Model Binding Source Attribute，以及复杂 Query DTO 缺少 FromQuery 时为什么可能返回 415。"
tags:
  - ASP.NET Core
  - Model Binding
  - FromQuery
  - FromBody
---

# ASP.NET Core 参数从哪里来：Body、Route、Query 与 Header

Controller Action 的参数可能来自 URL、Query String、JSON Body 或 Header。Model Binding Attribute 用于明确告诉 ASP.NET Core 到哪里寻找数据。

## 四种常见来源

```csharp
public async Task<IActionResult> UpdatePost(
    [FromRoute] int id,
    [FromBody] UpdateBlogPostDto dto,
    [FromQuery] bool publish,
    [FromHeader(Name = "If-Match")] string? version)
```

| Attribute | 数据来源 | 示例 |
|---|---|---|
| `[FromRoute]` | Route Segment | `/api/blogs/12` |
| `[FromQuery]` | Query String | `?page=2` |
| `[FromBody]` | Request Body | JSON DTO |
| `[FromHeader]` | HTTP Header | `If-Match` |

## `FromRoute`

```csharp
[HttpGet("{id:int}")]
public IActionResult GetPost([FromRoute] int id)
```

请求：

```http
GET /api/blogs/12
```

绑定结果：

```csharp
id = 12;
```

Route Template 中的名称应与参数一致，或通过 `Name` 显式指定。

## `FromBody`

```csharp
public IActionResult CreatePost(
    [FromBody] CreateBlogPostDto dto)
```

请求需要声明媒体类型：

```http
POST /api/blogs
Content-Type: application/json
```

```json
{
  "title": "First post",
  "content": "Body"
}
```

Input Formatter 根据 Content-Type 读取并反序列化 Body。一个 Action 通常只能有一个 `[FromBody]` 参数，因为 Request Body Stream 不能被多个 Formatter 独立消费。

## `FromQuery`

```csharp
public IActionResult GetPosts(
    [FromQuery] PaginationQueryDto pagination)
```

请求：

```http
GET /api/blogs?page=2&pageSize=10
```

绑定结果：

```csharp
pagination.Page = 2;
pagination.PageSize = 10;
```

如果 Query Parameter 缺失，使用 DTO Property 的默认值。

## 为什么 GET 会返回 415

在 `[ApiController]` 下，未标记来源的复杂类型可能被推断为 Body：

```csharp
GetPosts(PaginationQueryDto pagination)
```

GET 请求通常没有 JSON Body 和对应 Content-Type，于是 Input Formatter 无法处理，返回：

```text
415 Unsupported Media Type
```

最小修复：

```csharp
GetPosts([FromQuery] PaginationQueryDto pagination)
```

这不是 EF Core、分页 SQL 或 Database 错误，而是 Parameter Binding Source 错误。

## `FromHeader`

```csharp
public IActionResult UpdatePost(
    [FromHeader(Name = "If-Match")] string? version)
```

可以读取并发控制、Correlation ID 或客户端能力等 Header。Authentication Header 通常由 Authentication Middleware 处理，不需要每个 Action 手工解析。

## 是否必须显式写 Attribute

`[ApiController]` 会推断很多来源，例如 Route Template 中的简单参数通常来自 Route。

对于复杂 Query DTO，显式写 `[FromQuery]` 可以避免歧义，并让接口签名更容易阅读。

## 400、404 与 415 的区别

```text
400 → 已找到输入来源，但值格式或 Validation 失败
404 → 没有匹配 Route，或业务资源不存在
415 → Request Body 媒体类型无法处理
```

## 总结

遇到参数为默认值、DTO 全空或 415 时，应先检查 Binding Source：

```text
Route  → [FromRoute]
Query  → [FromQuery]
JSON   → [FromBody]
Header → [FromHeader]
```

## 参考资料

- [ASP.NET Core model binding](https://learn.microsoft.com/aspnet/core/mvc/models/model-binding)
- [Create web APIs with ApiController](https://learn.microsoft.com/aspnet/core/web-api/)
