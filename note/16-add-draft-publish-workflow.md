---
title: "为 ASP.NET Core 博客加入草稿、发布与取消发布"
description: "使用状态枚举、领域方法、EF Core Migration 和授权端点，让草稿只对管理员可见并支持幂等发布。"
tags:
  - ASP.NET Core
  - EF Core
  - Domain Model
  - Authorization
---

# 为 ASP.NET Core 博客加入草稿、发布与取消发布

上一篇已经使用 JWT Bearer 保护写接口，但所有文章仍只有“存在或不存在”两种状态。真正的博客需要先保存草稿，发布后才允许游客读取，也需要能够取消发布。

本文只解决文章发布生命周期，不处理定时发布或审核工作流。

## 定义状态与时间

在 `Models/BlogPostStatus.cs` 定义状态：

```csharp
public enum BlogPostStatus
{
    Draft,
    Published
}
```

在 `BlogPost` 增加：

```csharp
public BlogPostStatus Status { get; set; }
    = BlogPostStatus.Draft;

public DateTime? PublishedAtUtc { get; set; }
```

新文章默认是 Draft；只有 Published 才应该拥有发布时间。`PublishedAtUtc` 必须可空，因为草稿没有合法的发布时间。

## 让 Entity 维护状态一致性

把状态变化封装成方法，可以避免出现 `Status = Published` 但 `PublishedAtUtc = null` 的组合：

```csharp
public bool Publish()
{
    if (Status == BlogPostStatus.Published
        && PublishedAtUtc is not null)
    {
        return false;
    }

    Status = BlogPostStatus.Published;
    PublishedAtUtc = DateTime.UtcNow;
    return true;
}

public bool Unpublish()
{
    if (Status == BlogPostStatus.Draft
        && PublishedAtUtc is null)
    {
        return false;
    }

    Status = BlogPostStatus.Draft;
    PublishedAtUtc = null;
    return true;
}
```

返回值表示状态是否真的改变。重复 Publish 已发布文章会返回 `false`，不会修改首次发布时间，因此操作具有幂等性。

## 把枚举保存成可读字符串

在 `AppDbContext.OnModelCreating` 配置：

```csharp
modelBuilder.Entity<BlogPost>()
    .Property(post => post.Status)
    .HasConversion<string>()
    .HasMaxLength(20);
```

数据库会保存 `Draft` 或 `Published`，而不是依赖枚举数字。以后调整枚举顺序不会改变已有数据的含义。

## 安全迁移已有文章

为已有表增加必填字段时，不能直接要求旧行提供新值。安全顺序是：

```text
1. Status 暂时允许 null
2. 把旧文章回填为 Published
3. PublishedAtUtc 使用原 CreatedAtUtc
4. 再把 Status 改为 NOT NULL
```

生成并应用 Migration：

```bash
make migration NAME=AddBlogPostPublishing
make db-update
```

检查生成的 Migration，确认包含旧数据回填，而不是仅仅接受工具生成的 Schema 修改。

## Service 只在状态改变时保存

```csharp
if (post.Publish())
{
    await _dbContext.SaveChangesAsync();
}
```

取消发布同理调用 `post.Unpublish()`。如果文章不存在，Service 返回 `null`，Controller 转换为 `404 Not Found`。

## 添加受保护端点

```csharp
[Authorize(Roles = "Admin")]
[HttpPost("{id:int}/publish")]
public async Task<ActionResult<BlogPostResponseDto>>
    PublishPost(int id)
{
    var post = await _blogPostService.PublishPostAsync(id);
    return post is null ? NotFound() : Ok(post);
}
```

取消发布使用：

```http
POST /api/blogs/{id}/unpublish
```

两个端点都属于状态命令，不使用普通 `PUT` 替客户端直接覆盖所有字段。

## 游客只能查询已发布文章

查询基础方法根据权限决定是否包含草稿：

```csharp
if (!includeDrafts)
{
    query = query.Where(post =>
        post.Status == BlogPostStatus.Published);
}
```

Controller 使用 `User.IsInRole("Admin")` 传入 `includeDrafts`。游客读取草稿时返回 404，而不是 403，这样不会向外部确认某个私有 Slug 是否存在。

## 验证

完整流程应验证状态和可见性，而不只验证一次响应：

```text
Admin 创建文章       → Status=Draft
游客 GET             → 404
Admin Publish         → Status=Published，PublishedAtUtc 有值
游客 GET             → 200
Admin Unpublish       → Status=Draft，PublishedAtUtc=null
游客 GET             → 404
```

最后运行：

```bash
dotnet build
dotnet test
```

## 总结

文章已经拥有 Draft 与 Published 生命周期；Entity 维护状态组合，Service 编排持久化，Controller 负责授权和 HTTP Response，公共查询只返回已发布内容。

## 参考资料

- [EF Core value conversions](https://learn.microsoft.com/ef/core/modeling/value-conversions)
- [Role-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/roles)

## 主线导航

- 上一步：[使用 JWT Bearer 保护博客写接口](./15b-protect-blog-write-endpoints-jwt.md)
- 下一步：[使用 WebApplicationFactory 测试授权与发布流程](./17-test-blog-api-workflows.md)
