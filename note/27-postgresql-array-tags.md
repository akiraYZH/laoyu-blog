---
title: "使用 PostgreSQL 数组保存并筛选文章标签"
description: "通过 EF Core List<string> 到 PostgreSQL text[] 的映射，为文章加入标签、筛选参数和分类下的标签目录。"
tags:
  - EF Core
  - PostgreSQL
  - Tags
  - Vue
---

# 使用 PostgreSQL 数组保存并筛选文章标签

Category 是可统一管理、可复用的导航维度；Tag 是文章作者输入的轻量关键词。本项目保留 Category 多对多关系，同时用 PostgreSQL `text[]` 保存每篇文章的 Tags。

## 数据结构选择

```text
Category → 独立表 + Many-to-many，适合统一名称和管理
Tag      → BlogPosts.Tags text[]，适合数量较少的轻量筛选
```

数组方案减少了 Tag 与 Join Table，但也失去了独立 Tag 实体、别名、描述和统一重命名能力。若未来需要这些功能，应迁移到关系表。

## 修改 Entity 和 DTO

`Models/BlogPost.cs`：

```csharp
public List<string> Tags { get; set; } = [];
```

`BlogPostDto` 和 `BlogPostResponseDto` 也加入相同字段：

```csharp
public List<string> Tags { get; set; } = [];
```

创建与更新时显式复制：

```csharp
post.Tags = dto.Tags ?? [];
```

Response Projection 也必须映射 `Tags = post.Tags`，否则数据库已经保存，前端仍然看不到。

## 创建 Migration

```bash
make migration NAME=AddTagsToBlogPost
```

Npgsql 把 `List<string>` 映射为 PostgreSQL `text[]`：

```csharp
migrationBuilder.AddColumn<List<string>>(
    name: "Tags",
    table: "BlogPosts",
    type: "text[]",
    nullable: false,
    defaultValue: new List<string>());
```

Default Empty Array 让已有文章在增加 NOT NULL Column 后仍能迁移。检查 Migration 后再执行：

```bash
make db-update
```

Migration 会改变数据库 Schema；只修改 DTO、Query 或前端不需要再创建 Migration。

## 增加筛选参数

`PaginationQueryDto`：

```csharp
[StringLength(100)]
public string? Tag { get; set; }

public bool UntaggedOnly { get; set; }
```

Service 在分页前增加数据库筛选：

```csharp
if (!string.IsNullOrWhiteSpace(tag))
{
    query = query.Where(post => post.Tags.Contains(tag));
}

if (untaggedOnly)
{
    query = query.Where(post => post.Tags.Count == 0);
}
```

顺序仍然是 `Where → CountAsync → OrderBy → Skip → Take`。若分页之后才筛选，TotalItems 和页面内容都会错误。

当前查询是精确、区分大小写的数组元素匹配；`dotnet` 与 `.NET` 是不同 Tag。当前实现也没有在 API 层限制 Tag 数量或自动去重，这些是应在公开写入场景补充的 Validation 边界。

## 查询分类下可用的标签

```csharp
var tags = await query
    .Where(post =>
        post.Categories.Any(category =>
            category.Slug == categorySlug))
    .SelectMany(post => post.Tags)
    .Distinct()
    .OrderBy(tag => tag)
    .ToListAsync();
```

`BuildReadablePostsQuery(includeDrafts)` 先处理发布可见性，因此游客不会从 Tag 列表间接看到草稿关键词，Admin 则可以管理草稿。

Controller 暴露：

```http
GET /api/categories/{slug}/tags
```

## 前端把 Tag 表现为目录

编辑表单使用 Ant Design Vue 的 Tags Mode：

```vue
<a-select
  v-model:value="tags"
  mode="tags"
  placeholder="Create tags"
/>
```

分类页面先请求该分类的 Tags，并把每个 Tag 渲染为 Folder Card；进入目录后把 Tag 放入 URL Query：

```text
/categories/dotnet?tag=ef-core
```

URL 是筛选状态的单一来源，因此刷新、前进后退和分享链接仍能恢复同一个列表。分类根目录可以用 `untaggedOnly=true` 只显示未归入 Tag 目录的文章。

## 验证

1. 为一篇文章添加两个 Tags 并保存；
2. GET 文章，确认 Response 含 Tags；
3. 使用 `?tag=<tag>`，确认只返回精确匹配文章；
4. 使用 `?untaggedOnly=true`，确认只返回空数组文章；
5. 以游客身份查询分类 Tags，确认草稿 Tag 不泄漏；
6. 执行数据库查询确认 Column 类型：

```sql
SELECT "Id", "Title", "Tags"
FROM "BlogPosts"
ORDER BY "Id";
```

## 下一步改进

在数据量增加前，先补齐输入规范：Trim、去空白、去重、最大 Tag 数量和单个 Tag 长度。只有查询确实变慢后，再根据实际 Query Plan 评估 PostgreSQL GIN Index，不要因为使用数组就预先添加索引。

## 参考资料

- [Npgsql array mapping](https://www.npgsql.org/efcore/mapping/array.html)
- [EF Core migrations overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Vue Router query parameters](https://router.vuejs.org/guide/essentials/navigation.html)

## 主线导航

- 上一步：[保留 AWS 方案并部署到私人 VPS](./26-vps-deployment-profile.md)
- 下一步：[返回系列目录](./README.md)
