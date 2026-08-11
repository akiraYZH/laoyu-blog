---
title: "使用 System.Text.Json Attribute 控制 API JSON"
description: "使用 JsonPropertyName、JsonIgnore 和 JsonConverter 控制 C# DTO 与 JSON 之间的序列化形式。"
tags:
  - ASP.NET Core
  - System.Text.Json
  - DTO
  - Serialization
---

# 使用 System.Text.Json Attribute 控制 API JSON

C# Property Name 和 API JSON Contract 不一定相同。System.Text.Json Attribute 用于描述属性如何被序列化和反序列化。

```csharp
using System.Text.Json.Serialization;
```

## 默认命名策略

ASP.NET Core Web API 通常把 PascalCase Property 输出为 camelCase：

```csharp
public DateTime CreatedAtUtc { get; set; }
```

默认 JSON：

```json
{
  "createdAtUtc": "2026-08-10T00:00:00Z"
}
```

## `JsonPropertyName`

需要明确改变 JSON Field Name 时：

```csharp
[JsonPropertyName("created_at")]
public DateTime CreatedAtUtc { get; set; }
```

输出：

```json
{
  "created_at": "2026-08-10T00:00:00Z"
}
```

Attribute 影响 JSON Contract，不会修改 C# Property Name 或 Database Column Name。

## `JsonIgnore`

```csharp
[JsonIgnore]
public string InternalNote { get; set; } = string.Empty;
```

序列化时不会输出这个属性，反序列化时通常也不会从 JSON 读取它。

对于 Password Hash、Security Stamp 等敏感信息，更安全的方式是使用专门 Response DTO，让敏感属性根本不存在于可返回类型中，而不是只依赖 `[JsonIgnore]`。

## 条件忽略

```csharp
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string? Summary { get; set; }
```

当 Summary 为 `null` 时不输出该字段；有值时正常输出。

这只影响 Response JSON，不等于 `[Required]`，也不等于 Database `NOT NULL`。

## `JsonConverter`

需要自定义 Type 与 JSON 的转换方式时，可以指定 Converter：

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public PostStatus Status { get; set; }
```

Enum 可以输出为：

```json
{
  "status": "Published"
}
```

而不是整数值。

复杂 Converter 应独立实现和测试，不要把业务规则藏进序列化逻辑。

## Attribute 的边界

```text
JsonPropertyName → JSON Field Name
JsonIgnore       → 是否进入 JSON
JsonConverter    → 如何转换 JSON Value
```

它们不负责：

- API 请求是否有效；
- Database Column Mapping；
- 用户是否有权限；
- 数据是否唯一。

## Request DTO 与 Response DTO

当输入和输出差异明显时，应拆分：

```csharp
public class CreateBlogPostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class BlogPostResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
```

这比在一个大型 Entity 上不断添加 JSON Attribute 更容易控制 API Contract。

## 总结

System.Text.Json Attribute 是 JSON Metadata，不是 Validation 或 Database Mapping。先确定 API 希望暴露什么，再决定使用 Attribute 还是独立 Response DTO。

## 参考资料

- [Customize JSON property names](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/customize-properties)
- [Ignore properties](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/ignore-properties)
- [Custom converters](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/converters-how-to)

