---
title: "使用 EF Core 为博客文章加入多分类"
description: "建立 BlogPost 与 Category 的多对多关系，创建 Join Table 和唯一 Slug Index，并通过 DTO 暴露分类数据。"
tags:
  - EF Core
  - PostgreSQL
  - Many-to-many
  - DTO
  - Migration
---

# 使用 EF Core 为博客文章加入多分类

一篇文章可以属于多个分类，一个分类也可以包含多篇文章，因此 `BlogPost` 和 `Category` 不是一对多，而是多对多关系。

本文只建立数据模型、数据库关系和 API Contract。下一篇再实现“分类不存在时自动创建”和按分类筛选文章。

## 前置状态

项目已经具有 `BlogPost` Entity、CRUD、Response DTO、Service Layer 和 EF Core Migration 工具链。

## 创建 Category Entity

新建 `Models/Category.cs`：

```csharp
namespace BlogApi.Models;

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<BlogPost> BlogPosts { get; set; }
        = new List<BlogPost>();
}
```

`BlogPosts` 是 Navigation Property，表示当前分类关联的文章集合。

## 修改 BlogPost

在 `Models/BlogPost.cs` 加入反向 Navigation Property：

```csharp
public ICollection<Category> Categories { get; set; }
    = new List<Category>();
```

两个集合表达：

```text
BlogPost.Categories  → 一篇文章拥有多个分类
Category.BlogPosts   → 一个分类拥有多篇文章
```

## 配置多对多关系和唯一索引

在 `AppDbContext` 中增加：

```csharp
public DbSet<Category> Categories => Set<Category>();
```

在 `OnModelCreating` 中：

```csharp
modelBuilder.Entity<Category>()
    .HasIndex(category => category.Slug)
    .IsUnique();

modelBuilder.Entity<BlogPost>()
    .HasMany(post => post.Categories)
    .WithMany(category => category.BlogPosts)
    .UsingEntity("BlogPostCategories");
```

`HasMany(post => post.Categories)` 已经从 `BlogPost` 的属性类型确定目标 Entity 是 `Category`。因此下一行 Lambda 中的 `category` 参数类型也是 `Category`：

```csharp
.WithMany(category => category.BlogPosts)
```

变量名可以改成其他名称，真正决定类型的是前一段关系表达式和属性类型。

`UsingEntity("BlogPostCategories")` 指定中间表名称。中间表保存两边的主键，而不是在任一表中塞入逗号分隔的分类 ID。

## 更新 DTO

创建 `Dtos/CategoryResponseDto.cs`：

```csharp
namespace BlogApi.Dtos;

public sealed class CategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
```

在 `BlogPostResponseDto` 中加入：

```csharp
public List<CategoryResponseDto> Categories { get; set; } = [];
```

创建或更新文章时，客户端提交分类名称。在 `BlogPostDto` 中加入：

```csharp
[Required(ErrorMessage = "Categories are required.")]
public List<string> CategoryNames { get; set; } = [];
```

集合级规则可以通过 `IValidatableObject` 表达，例如最多十个分类，以及每个名称必须包含字母或数字。DTO 负责验证输入形状；查询已有分类、创建缺失分类属于 Service 的业务流程。

## 创建 Migration

```bash
make migration NAME=AddCategories
make db-update
```

Migration 应创建：

```text
Categories
BlogPostCategories
IX_Categories_Slug UNIQUE
```

其中 `BlogPostCategories` 的组合主键防止同一文章重复关联同一分类。

## 验证

```bash
dotnet build
```

再检查 Migration 和数据库：

1. `Categories` 包含 `Id`、`Name` 和 `Slug`；
2. `BlogPostCategories` 同时包含文章和分类外键；
3. 重复的 Category Slug 会被 Unique Index 拒绝。

本文尚未实现创建分类的 HTTP 流程；数据库表为空是正常状态。

## 常见错误

### 只增加 ICollection，没有 Migration

Entity 改变不会自动修改数据库。必须生成并应用 Migration。

### 把 Category Entity 直接作为 API Response

Response DTO 应继续隔离 EF Core Entity，避免 Navigation Property 造成循环序列化或意外暴露持久化结构。

## 完成状态

```text
BlogPost ← BlogPostCategories → Category
Category.Slug → Unique Index
BlogPostDto → 接收 CategoryNames
BlogPostResponseDto → 返回 CategoryResponseDto
```

## 参考资料

- [EF Core Relationships](https://learn.microsoft.com/ef/core/modeling/relationships)
- [EF Core Indexes](https://learn.microsoft.com/ef/core/modeling/indexes)

## 主线导航

- 上一步：[使用 IExceptionHandler 统一处理 Slug 冲突](./13-aspnet-core-iexceptionhandler-postgresql-conflict.md)
- 下一步：[自动创建分类并按分类筛选文章](./14b-create-and-filter-blog-categories.md)
