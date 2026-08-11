---
title: "ASP.NET Core：什么时候应该加入 Service Layer"
description: "从简单 CRUD 到分页和业务规则，判断何时把逻辑从 Controller 移到 Service，而不是为了目录结构提前增加抽象。"
tags:
  - ASP.NET Core
  - Service Layer
  - Dependency Injection
  - Architecture
---

# ASP.NET Core：什么时候应该加入 Service Layer

Service 不是 ASP.NET Core Controller 的必需组件。一个只有少量 CRUD 的 API，可以直接通过 `DbContext` 完成查询和保存。

真正的问题不是“项目有没有分页”，而是：

> Controller 是否开始承担越来越多与 HTTP 无关的逻辑？

## Controller 应该负责什么

Controller 位于 HTTP 边界，适合负责：

- 从 Route、Query 和 Body 绑定参数；
- 调用应用逻辑；
- 返回 `200`、`404`、`409` 等状态码；
- 生成 HTTP Response。

例如：

```csharp
[HttpGet]
public async Task<IActionResult> GetPosts(
    [FromQuery] PaginationQueryDto pagination)
{
    var result = await blogPostService.GetPostsAsync(
        pagination.Page,
        pagination.PageSize);

    return Ok(result);
}
```

## Service 应该负责什么

Service 适合负责与 HTTP 无关的应用用例：

- 构造分页和筛选查询；
- 执行创建、发布或归档流程；
- 组合多个 Entity 或外部服务；
- 实现需要复用的业务规则；
- 定义事务边界。

分页 Service 可以接收普通参数：

```csharp
public async Task<PagedResultDto<BlogPost>> GetPostsAsync(
    int page,
    int pageSize)
{
    // 构造并执行 EF Core 查询
}
```

Service 不需要知道参数来自 Query String，也不应该直接返回 `Ok()` 或 `NotFound()`。

## 什么时候还不需要 Service

如果 Service 只是机械复制 Controller 中的一行代码：

```csharp
public Task<BlogPost?> GetAsync(int id)
    => dbContext.BlogPosts.FindAsync(id).AsTask();
```

但没有复用、业务规则或测试边界，它只增加了跳转文件的成本。

简单 CRUD 阶段可以先保持直接，再根据真实复杂度重构。

## 什么时候应该加入

以下信号出现两三个时，就适合加入 Service：

- 同一业务操作被多个 Endpoint 使用；
- Controller 同时包含查询、计算、异常转换和状态码；
- 保存一次请求需要修改多个 Entity；
- 业务规则需要独立测试；
- Controller 已经难以快速读懂；
- 后续还会增加发布、权限或缓存。

分页本身不是强制信号，但分页、Slug 冲突、发布状态和权限不断叠加时，Service 就开始提供明确价值。

## 如何注册 Service

依赖 `DbContext` 的 Service 通常注册为 Scoped：

```csharp
builder.Services.AddScoped<BlogPostService>();
```

Controller 通过构造函数接收它：

```csharp
public BlogsController(BlogPostService blogPostService)
{
    _blogPostService = blogPostService;
}
```

同一个 HTTP Request 中，Scoped Service 会使用同一请求范围内的 Scoped `DbContext`。

## 是否还需要 Repository

使用 EF Core 时，不应该仅为了架构图再包装一层通用 Repository。`DbContext` 和 `DbSet<T>` 已经提供查询、状态跟踪和 Unit of Work 能力。

只有需要隐藏特殊数据源、复用复杂查询或替换持久化边界时，额外 Repository 才可能有明确价值。

## 是否需要 Interface

如果只有一个实现：

```csharp
BlogPostService
```

可以先直接注册具体类型。只有存在替代实现、清晰测试边界或跨程序集契约时，再引入：

```csharp
IBlogPostService
```

Interface 不是 Service 的入场券。

## 渐进式重构

可以先移动一个完整用例，例如分页查询：

```text
先移动 GetPosts
确认响应不变
再移动 Create、Update、Delete
最后从 Controller 移除 DbContext
```

重构的验证标准是外部行为保持不变，而不是一次改完所有文件。

## 总结

```text
Controller → HTTP 输入和输出
Service    → 应用用例与业务规则
DbContext  → EF Core 数据访问与状态跟踪
Database   → 数据持久化和最终约束
```

Service 应该在复杂度出现时解决真实问题，而不是为了让目录看起来更像企业项目。

## 参考资料

- [ASP.NET Core Dependency Injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [EF Core DbContext](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
