---
title: "在 ASP.NET Core 中配置 EF Core DbContext"
description: "从 Connection String、Entity、DbSet 到 AddDbContext 与 UseNpgsql，建立 API 访问 PostgreSQL 的完整配置链路。"
tags:
  - ASP.NET Core
  - EF Core
  - PostgreSQL
  - DbContext
  - Npgsql
---

# 在 ASP.NET Core 中配置 EF Core DbContext

PostgreSQL Container 已启动，不代表 API 已经能够访问数据库。应用还需要 Connection String、Entity、DbContext 和 Npgsql Provider 共同组成数据访问链路。

```text
Controller 或 Service
        ↓
AppDbContext
        ↓
EF Core
        ↓
Npgsql
        ↓
PostgreSQL
```

## 定义 Entity

创建 `Models/BlogPost.cs`：

```csharp
namespace BlogApi.Models;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

Entity 描述应用中的数据库对象。EF Core 按 Convention 推断：

- `Id` 是 Primary Key；
- 非 Nullable String 映射为必填 Column；
- `DateTime` 映射为时间类型。

`= string.Empty` 主要满足 C# Nullable Analysis，不等于完整业务验证。

## 创建 DbContext

创建 `Data/AppDbContext.cs`：

```csharp
using BlogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
}
```

`AppDbContext` 是应用使用 EF Core 的入口，负责 Query、Change Tracking 和 `SaveChangesAsync()`。

## `DbContextOptions`

```csharp
DbContextOptions<AppDbContext> options
```

Options 保存 Provider、Connection String 和其他 EF Core 配置。

```csharp
: base(options)
```

把这些配置交给父类 `DbContext`。构造函数本身可以为空，因为初始化工作由 Base Class 完成。

## `DbSet<BlogPost>`

```csharp
public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
```

`DbSet` 是 EF Core 查询和修改某类 Entity 的入口：

```csharp
await dbContext.BlogPosts.ToListAsync();
dbContext.BlogPosts.Add(post);
dbContext.BlogPosts.Remove(post);
```

它可以近似理解为 Table 操作入口，但还包含 LINQ Query Construction 和 Entity Tracking 能力。

## 准备 Connection String

在 Configuration 中提供：

```text
ConnectionStrings:DefaultConnection
```

### API 运行在开发机上

ASP.NET Core 默认不会自动读取 `.env.development`。在当前 Terminal 中加载数据库变量，并显式准备 Connection String：

```bash
set -a
source .env.development
set +a

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
```

这里使用 `Host=localhost`，因为 .NET Process 运行在开发机上，通过 Compose 发布的端口连接 PostgreSQL。

Environment Variable 只在当前 Terminal Session 及其 Child Process 中有效。不要把真实密码写进提交到 Git 的配置文件。

### API 运行在 Compose 中

Container Environment Variable 可以写成：

```yaml
environment:
  ConnectionStrings__DefaultConnection: >-
    Host=postgres;Port=5432;Database=blog_dev;
    Username=blog_user;Password=local_password
```

双下划线会映射为 Configuration 层级分隔符：

```text
ConnectionStrings__DefaultConnection
                ↓
ConnectionStrings:DefaultConnection
```

API 运行在 Compose Network 中时使用 `Host=postgres`；开发机上的 CLI 使用端口映射，因此通常使用 `Host=localhost`。

## 注册 DbContext 与 Npgsql

在 `Program.cs` 中：

```csharp
using BlogApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

职责分解：

```text
GetConnectionString → 读取配置
AddDbContext        → 注册 AppDbContext
UseNpgsql           → 选择 PostgreSQL Provider
```

缺少 Connection String 时立即抛出清晰异常，比第一次 Query 时才失败更容易诊断。

## Scoped Lifetime

`AddDbContext` 默认把 DbContext 注册为 Scoped。通常每个 HTTP Request 获得一个 DbContext 实例，请求结束后释放。

不要把 DbContext 注册为 Singleton。DbContext 维护 Change Tracking State，也不是 Thread-safe 的全局共享对象。

## 在 Controller 中使用

```csharp
public class BlogsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public BlogsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
```

ASP.NET Core Dependency Injection 会创建配置好的 AppDbContext，并传入 Controller Constructor，不需要手写 `new AppDbContext(...)`。

## 验证

```bash
dotnet build
```

启动应用后执行一个最小数据库 Query。仅仅看到 API 启动并不能完全证明 Database Connection 有效，因为 EF Core 通常在真正执行 Query 时才打开连接。

## 总结

```text
Entity          → 数据长什么样
DbSet           → 操作某类 Entity 的入口
DbContext       → EF Core 工作单元
ConnectionString→ 数据库在哪里
UseNpgsql       → 使用 PostgreSQL Provider
AddDbContext    → 注册进 Dependency Injection
```

## 参考资料

- [EF Core DbContext configuration](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [ASP.NET Core configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)

## 主线导航

- 上一步：[启动 PostgreSQL 开发数据库](./02-docker-compose-postgresql-development.md)
- 下一步：[创建第一份 EF Core Migration](./04b-create-first-ef-core-migration.md)
