---
title: "使用 DataAnnotations 验证 ASP.NET Core API 请求"
description: "掌握 Required、StringLength、Range、RegularExpression 等常用验证 Attribute，以及 ApiController 自动返回 400 的过程。"
tags:
  - ASP.NET Core
  - DTO
  - DataAnnotations
  - Validation
---

# 使用 DataAnnotations 验证 ASP.NET Core API 请求

DTO 定义客户端可以提交的数据。DataAnnotations 可以给 DTO 属性附加验证元数据，让 ASP.NET Core 在调用 Controller Action 前拒绝无效请求。

## 什么是验证元数据

```csharp
[Required]
[StringLength(200)]
public string Title { get; set; } = string.Empty;
```

`Title` 是数据；`[Required]` 和 `[StringLength]` 是描述这份数据规则的 Metadata。

Attribute 不会修改字符串本身，而是让 Validation Framework 知道应该执行哪些检查。

## 常用 Attribute

```csharp
using System.ComponentModel.DataAnnotations;
```

| Attribute | 解决的问题 |
|---|---|
| `[Required]` | 字段是否必须提供 |
| `[StringLength]` | String 长度范围 |
| `[MinLength]` | String、Array 或 Collection 最小长度 |
| `[MaxLength]` | String、Array 或 Collection 最大长度 |
| `[Range]` | Number 范围 |
| `[EmailAddress]` | Email 格式 |
| `[RegularExpression]` | 自定义正则格式 |
| `[Compare]` | 两个属性是否相同 |
| `[Url]` | URL 格式 |

## 文章请求示例

```csharp
public class BlogPostDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(
        200,
        MinimumLength = 1,
        ErrorMessage =
            "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}
```

此时文章模型还只有 Title 和 Content。后续加入 Slug 时，再为新字段增加 `RegularExpression`，避免文档提前依赖尚未创建的属性。

## `[ApiController]` 自动返回 400

Controller 标记：

```csharp
[ApiController]
public class BlogsController : ControllerBase
```

验证过程：

```text
读取 JSON
    ↓
创建 DTO
    ↓
执行 Attribute Validation
    ├─ 有效 → 调用 Action
    └─ 无效 → 自动返回 400
```

因此不需要重复写：

```csharp
if (!ModelState.IsValid)
{
    return BadRequest(ModelState);
}
```

默认响应类似：

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required."]
  }
}
```

验证失败时，Action 和 `SaveChangesAsync()` 都不会执行。

## Nullable 与 Required 不相同

```csharp
public string? Summary { get; set; }
```

`?` 主要影响 C# Nullable Analysis。

```csharp
[Required]
public string Summary { get; set; } = string.Empty;
```

`[Required]` 参与 API Model Validation。

数据库是否允许 `NULL` 则由 EF Core Model 和 Database Schema 决定。这三层相关，但不能互相替代。

## Validation 不等于业务规则

DataAnnotations 适合单字段、无外部依赖的规则。

以下规则不适合只写成 Attribute：

- Slug 在数据库中必须唯一；
- 当前用户必须拥有文章；
- 已发布文章不能修改某些字段；
- EndDate 必须结合其他数据判断。

需要 Database 或 Current User 的规则通常放在 Service，并由 Database Constraint 保护最终一致性。

## Create、PUT 与 PATCH DTO

不同操作可能需要不同规则：

```text
Create DTO → 创建时必填字段
PUT DTO    → 完整替换所需字段
PATCH DTO  → 只包含要修改的字段
```

项目很小时可以复用；当 Optional Field 和 Validation 开始分叉时，应拆分 DTO，而不是堆叠条件判断。

## 总结

```text
DataAnnotations → 描述请求字段规则
ApiController   → Action 前自动验证
Invalid Request → 自动返回 400
Service         → 处理跨字段和业务规则
Database        → 执行最终数据约束
```

## 参考资料

- [ASP.NET Core model validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation)
- [System.ComponentModel.DataAnnotations](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations)

## 主线导航

- 上一步：[实现 REST CRUD](./07-aspnet-core-ef-core-crud-api.md)
- 下一步：[构建 API Docker Image](./03-containerize-aspnet-core-api-with-docker.md)
