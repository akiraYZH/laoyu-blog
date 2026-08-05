---
title: "ASP.NET Core 使用 EF Core 连接 PostgreSQL"
description: "为 .NET 10 API 配置 PostgreSQL Connection String，创建 Entity 与 DbContext，注册 Npgsql，并生成第一个 Migration。"
tags:
  - .NET 10
  - ASP.NET Core
  - Entity Framework Core
  - PostgreSQL
  - Npgsql
  - Migration
---

# ASP.NET Core 使用 EF Core 连接 PostgreSQL

PostgreSQL 和 API Container 都能启动，不代表 API 已经具备数据库访问能力。中间还需要完成一条配置链路：

```text
Connection String
        ↓
Npgsql Provider
        ↓
AppDbContext
        ↓
BlogPost Entity
        ↓
Migration
```

本文完成这条链路，但不会直接修改数据库。Migration 生成后应先检查，再执行 `database update`。

## 准备工作

项目需要以下 Package 和 Local Tool：

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet new tool-manifest
dotnet tool install dotnet-ef
```

它们的职责不同：

| 组件 | 作用 |
|---|---|
| EF Core | Entity、DbContext、查询、状态跟踪和 Migration |
| Npgsql EF Core Provider | 把 EF Core 操作转换为 PostgreSQL 操作 |
| Npgsql | 与 PostgreSQL 进行底层通信 |
| EF Core Design | 支持 Migration 等设计时功能 |
| dotnet-ef | 执行 EF Core CLI 命令 |

`Npgsql.EntityFrameworkCore.PostgreSQL` 会把 EF Core 和 Npgsql 作为传递依赖带入项目，不需要重复安装 `Microsoft.EntityFrameworkCore`。

## 1. 把 Connection String 传入 API

在 `compose.yaml` 的 API Service 中加入：

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
    depends_on:
      postgres:
        condition: service_healthy
```

数据库名、用户和密码来自没有提交到 Git 的 `.env.development`：

```dotenv
POSTGRES_DB=blog_dev
POSTGRES_USER=blog_user
POSTGRES_PASSWORD=replace_with_local_password
```

这里有两个关键点。

第一，ASP.NET Core 会把环境变量中的双下划线转换为配置层级：

```text
ConnectionStrings__DefaultConnection
                    ↓
ConnectionStrings:DefaultConnection
```

所以 C# 可以这样读取：

```csharp
builder.Configuration.GetConnectionString("DefaultConnection");
```

第二，API 运行在 Compose Network 中，因此数据库 Host 是 PostgreSQL 的 Service Name：

```text
Host=postgres
```

Container 中的 `localhost` 指向 Container 自己，不是另一个 Container。

修改后先检查 Compose 配置：

```bash
docker compose --env-file .env.development config --quiet
```

## 2. 创建 BlogPost Entity

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

EF Core 会按照约定解释这些属性：

- `Id` 是 Primary Key；
- 非 Nullable 的 `Title` 和 `Content` 是必填字段；
- `CreatedAtUtc` 保存文章创建时间。

`= string.Empty` 解决的是 C# Nullable Warning，不是业务验证。是否允许空标题，仍应由请求验证或业务规则决定。

## 3. 创建 AppDbContext

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

`AppDbContext` 是 API 使用 EF Core 的入口。它管理数据库配置、查询、修改跟踪和保存操作。

构造函数中的：

```csharp
: base(options)
```

表示先调用父类 `DbContext` 的构造函数，把数据库 Provider 和 Connection String 等配置交给 EF Core。

```csharp
public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
```

则把 `BlogPost` 加入 EF Core Model。以后可以通过 `dbContext.BlogPosts` 查询和修改文章。

此时仍然没有创建数据库表。

## 4. 注册 AppDbContext 和 Npgsql

在 `Program.cs` 顶部加入：

```csharp
using BlogApi.Data;
using Microsoft.EntityFrameworkCore;
```

然后在创建 Builder 后加入：

```csharp
var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

这里的分工是：

```text
GetConnectionString
读取配置

AddDbContext
把 AppDbContext 注册进 ASP.NET Core DI

UseNpgsql
指定 PostgreSQL Provider
```

`?? throw` 让应用在缺少 Connection String 时立即停止，并给出明确错误。相比第一次查询时才失败，这更容易排查。

`AddDbContext` 默认使用 Scoped Lifetime，通常每个 HTTP Request 获得一个独立的 `AppDbContext`。

## 5. 验证编译和运行时配置

先检查代码：

```bash
dotnet build
```

然后用最新源码重新构建 API Image：

```bash
docker compose \
  --env-file .env.development \
  up --detach --build
```

查看日志：

```bash
docker compose \
  --env-file .env.development \
  logs api --tail 30
```

预期看到：

```text
Now listening on: http://[::]:8080
Application started.
Hosting environment: Development
```

如果没有出现：

```text
Connection string 'DefaultConnection' was not found.
```

说明 API Container 已经读取到 Connection String。

不过应用能启动不代表表已经存在。只有真正查询数据库或应用 Migration，才能验证数据库连接和 Schema。

## 6. 创建第一个 Migration

Migration 是数据库结构的版本记录。它把当前 EF Core Model 转换成可检查、可提交到 Git 的 Schema Change。

项目提供了 Wrapper Script，所以只需执行：

```bash
./scripts/add-migration.sh InitialCreate
```

脚本会自动：

- 读取 `.env.development`；
- 为开发机生成 `Host=localhost` 的 Connection String；
- 使用项目级 `dotnet-ef`；
- 指定 `AppDbContext`；
- 把结果写入 `Data/Migrations`。

为什么脚本使用 `localhost`，而 API 使用 `postgres`？

```text
API Container → postgres:5432
开发机脚本   → localhost:5432
```

两条命令运行在不同网络环境中。

成功后通常生成：

```text
Data/Migrations/
├── 时间戳_InitialCreate.cs
├── 时间戳_InitialCreate.Designer.cs
└── AppDbContextModelSnapshot.cs
```

其中最需要检查的是 Migration 主文件：

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 应该只创建当前模型需要的表、字段和约束
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // 应该能够撤销 Up() 的结构变更
}
```

`migrations add` 只生成文件，不会修改 PostgreSQL。检查正确后，下一步才是：

```bash
dotnet ef database update
```

## 为什么企业仍然使用 Migration

手动修改数据库可能很快，但无法可靠回答每个环境执行过哪些变更。Migration 把 Schema Change 保存进代码仓库，让开发、测试和生产环境使用同一套版本记录。

常见企业流程是：

```text
修改 Entity
    ↓
生成并检查 Migration
    ↓
提交 Pull Request
    ↓
CI 构建和测试
    ↓
部署流程应用 Migration
```

企业通常会用 Script、Makefile 或内部工具简化命令，而不是取消 Migration。

Production 也不应让多个 API 实例在启动时同时修改 Schema。更稳妥的方式是由 CI/CD、一次性 Migration Job 或 EF Core Migration Bundle 单独执行。

## 当前检查点

到这里已经完成：

- Connection String 进入 API Container；
- 创建 `BlogPost`；
- 创建并注册 `AppDbContext`；
- 配置 Npgsql；
- API Container 成功启动；
- 准备通过 Script 生成 `InitialCreate`。

下一步是检查生成的 Migration，然后将它应用到 Development PostgreSQL。

Shell Script 的实现单独放在下一篇：[从重复命令到项目脚本](./05-safe-bash-automation-for-dotnet.md)。

## 参考资料

- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [EF Core DbContext configuration](https://learn.microsoft.com/ef/core/dbcontext-configuration/)
- [EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)

