---
title: "EF Core 的 OnModelCreating 是什么"
description: "理解 OnModelCreating Hook、ModelBuilder Fluent API，以及数据库模型配置与 DTO Validation 的边界。"
tags:
  - EF Core
  - OnModelCreating
  - ModelBuilder
  - Database Constraint
---

# EF Core 的 OnModelCreating 是什么

EF Core 能通过 Convention 推断许多数据库结构，但 Index、复杂关系、Column Name 和删除行为等规则需要显式配置。`OnModelCreating` 就是应用加入这些配置的 Hook。

## 方法签名

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
}
```

它是 `DbContext` 提供的可重写生命周期方法。EF Core 建立 Model 时调用它，让应用补充 Entity Mapping。

它不会在每次 HTTP Request 或每次 Query 时执行。EF Core 通常会构建并缓存 Model，Migration Tool 也会读取这个 Model。

## `override`

```csharp
protected override void OnModelCreating(...)
```

`override` 表示当前 DbContext 为 Base Class 已定义的 Virtual Method 提供新实现。

```csharp
base.OnModelCreating(modelBuilder);
```

保留 Base Class 的模型配置，再加入当前应用的规则。

## 配置唯一索引

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<BlogPost>()
        .HasIndex(post => post.Slug)
        .IsUnique();
}
```

逐步解释：

```text
Entity<BlogPost>()          → 选择 BlogPost Entity
HasIndex(post => post.Slug)→ 为 Slug 配置 Index
IsUnique()                  → 非 NULL 值必须唯一
```

数据库会利用 Index 加快 Slug Query，并拒绝重复的非 NULL Slug。

## 常见配置

```csharp
modelBuilder.Entity<BlogPost>(entity =>
{
    entity.ToTable("BlogPosts");

    entity.HasKey(post => post.Id);

    entity.Property(post => post.Title)
        .HasMaxLength(200)
        .IsRequired();

    entity.HasIndex(post => post.Slug)
        .IsUnique();
});
```

Fluent API 可以配置：

- Primary Key；
- Index 和 Unique Index；
- Column Name、Type、Length 和 Default Value；
- Required/Nullable；
- Foreign Key 和 Navigation Relationship；
- Cascade Delete Behavior。

## 它会立即修改数据库吗

不会。

`OnModelCreating` 改变的是 EF Core Model：

```text
修改 OnModelCreating
        ↓
EF Core Model 改变
        ↓
生成 Migration
        ↓
应用 Migration
        ↓
PostgreSQL Schema 改变
```

只修改代码而不应用 Migration，数据库不会自动出现 Index 或新 Column。

## 与 DTO Validation 的区别

```text
DTO Attribute    → API Request 格式是否合法
Service          → 业务是否允许当前操作
OnModelCreating  → 数据库 Model 与最终 Constraint
```

例如：

```csharp
[Required]
[RegularExpression("^[a-z0-9-]+$")]
public string Slug { get; set; } = string.Empty;
```

DTO 可以提前返回友好的 `400`，但并发请求下的最终唯一性仍必须依赖 Database Unique Index。

## 总结

`OnModelCreating` 不是普通查询方法，而是 EF Core 构建 Model 时提供的配置 Hook。它负责描述 Entity 如何映射到数据库，真正的 Schema Change 仍由 Migration 应用。

## 参考资料

- [EF Core model building](https://learn.microsoft.com/ef/core/modeling/)
- [EF Core indexes](https://learn.microsoft.com/ef/core/modeling/indexes)

