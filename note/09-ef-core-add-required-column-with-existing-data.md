---
title: "EF Core：为已有数据的表安全添加必填字段"
description: "使用 Nullable Column、数据回填和 NOT NULL 三阶段 Migration，在不删除旧数据的情况下为 PostgreSQL 表添加必填 Slug。"
tags:
  - EF Core
  - PostgreSQL
  - Migration
  - Database Schema
---

# EF Core：为已有数据的表安全添加必填字段

为一张空表添加必填字段很简单；真正的问题是表中已经存在数据时，旧记录没有这个字段的值。

假设文章表已经包含数据，现在需要增加必填 `Slug`：

```csharp
public string Slug { get; set; } = string.Empty;
```

如果直接生成 `NOT NULL` Column，PostgreSQL 无法判断旧文章应该填什么，Migration 可能失败。安全做法是分三步完成。

## 第一步：先添加 Nullable Column

Entity 暂时允许 `null`：

```csharp
public string? Slug { get; set; }
```

并配置唯一索引：

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<BlogPost>()
        .HasIndex(post => post.Slug)
        .IsUnique();
}
```

生成的 Migration 应包含：

```csharp
migrationBuilder.AddColumn<string>(
    name: "Slug",
    table: "BlogPosts",
    type: "text",
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_BlogPosts_Slug",
    table: "BlogPosts",
    column: "Slug",
    unique: true);
```

旧文章的 `Slug` 会暂时成为 `NULL`，因此不会阻止 Column 创建。PostgreSQL 的普通 Unique Index 允许存在多个 `NULL`，但会拒绝重复的非空值。

生成、检查并应用这一阶段：

```bash
make migration NAME=AddNullableSlug
make db-update
```

应用前应确认 `Up()` 只包含 Nullable Slug Column 和 Unique Index。

## 第二步：回填旧数据

模型没有再次变化时，可以创建一份空 Migration，并在 `Up()` 中加入数据更新 SQL：

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """
        UPDATE "BlogPosts"
        SET "Slug" = 'post-' || "Id"::text
        WHERE "Slug" IS NULL;
        """);
}
```

结果类似：

```text
Id 2 → post-2
Id 3 → post-3
```

这里使用 ID 是因为它天然唯一，而且即使旧文章标题为空，也能得到稳定值。

`Down()` 只撤销本次生成的占位值：

```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """
        UPDATE "BlogPosts"
        SET "Slug" = NULL
        WHERE "Slug" = 'post-' || "Id"::text;
        """);
}
```

直接在本地数据库手动执行一次 UPDATE 并不够，因为测试和生产环境仍然缺少同样的数据变更。把回填写进 Migration，才能随 Schema Change 一起重复应用。

生成空 Migration：

```bash
make migration NAME=BackfillBlogPostSlugs
```

把 SQL 写入 `Up()` 和 `Down()`，检查后应用：

```bash
make db-update
```

验证：

```sql
SELECT "Id", "Title", "Slug"
FROM "BlogPosts"
ORDER BY "Id";
```

确认不存在 `Slug IS NULL` 的记录，再进入第三步。

## 第三步：改成 NOT NULL

确认所有旧文章都已有 Slug 后，再修改 Entity：

```csharp
public string Slug { get; set; } = string.Empty;
```

下一份 Migration 将 Column 改成非空：

```csharp
migrationBuilder.AlterColumn<string>(
    name: "Slug",
    table: "BlogPosts",
    type: "text",
    nullable: false,
    oldClrType: typeof(string),
    oldType: "text",
    oldNullable: true);
```

最终数据库同时拥有：

```text
NOT NULL     → Slug 不能缺失
UNIQUE INDEX → Slug 不能重复
```

生成并应用最后一份 Migration：

```bash
make migration NAME=MakeSlugRequired
make db-update
```

应用前确认 `AlterColumn` 的 `nullable` 已从 `true` 变成 `false`，并且没有意外删除其他 Column。

## 为什么不修改 InitialCreate

如果数据库从未部署、也没有需要保留的数据，可以修改初始设计并重建数据库。

一旦多个环境已经应用 `InitialCreate`，就不应该重写历史 Migration。新的 Migration 能明确记录数据库如何从旧版本升级到新版本，也不会要求删除已有数据。

## 总结

为已有表添加必填字段时，安全顺序是：

```text
添加 Nullable Column
        ↓
回填所有旧数据
        ↓
改成 NOT NULL
```

Migration 的价值不是要求第一次就想到所有字段，而是让不断变化的数据库结构可以安全、可追踪地升级。

## 参考资料

- [EF Core Migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [EF Core Indexes](https://learn.microsoft.com/ef/core/modeling/indexes)

## 主线导航

- 上一步：[配置 Container 热更新](./06-aspnet-core-docker-compose-hot-reload.md)
- 下一步：[把 Slug 接入文章 API](./09a-add-slug-to-blog-api.md)
