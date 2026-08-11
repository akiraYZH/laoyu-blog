---
title: "PostgreSQL、EF Core、Npgsql、Docker 与 Migration 分别负责什么"
description: "厘清 ASP.NET Core 持久化链路中数据库、ORM、Provider、Container 和 Schema Versioning 的职责。"
tags:
  - ASP.NET Core
  - PostgreSQL
  - EF Core
  - Npgsql
  - Docker
---

# PostgreSQL、EF Core、Npgsql、Docker 与 Migration 分别负责什么

接入数据库时最容易混淆的问题不是命令，而是每个组件究竟解决什么问题。

完整链路是：

```text
ASP.NET Core
      ↓
EF Core
      ↓
Npgsql Provider
      ↓
PostgreSQL
```

Docker 负责运行环境，Migration 负责数据库结构版本，它们位于不同维度。

## PostgreSQL：保存和约束数据

PostgreSQL 是真正的 Database Server，负责：

- 存储表和记录；
- 执行 SQL；
- 管理 Transaction；
- 执行 Primary Key、Foreign Key、Unique 和 NOT NULL Constraint；
- 建立 Index；
- 管理 Database User 和 Permission。

只安装 C# Package 不会产生 PostgreSQL Server。连接字符串指向的位置必须确实有 PostgreSQL 在监听。

## EF Core：用 .NET 对象表达数据库操作

EF Core 是 ORM。它让应用使用 Entity 和 LINQ 构造查询：

```csharp
var posts = await dbContext.BlogPosts
    .Where(post => post.Title.Contains("Docker"))
    .ToListAsync();
```

EF Core 负责：

- Entity Mapping；
- LINQ Query Translation；
- Change Tracking；
- `SaveChangesAsync()`；
- Migration Model Difference。

EF Core 不是数据库，也不会在本机启动 PostgreSQL。

## Npgsql：连接 EF Core 与 PostgreSQL

EF Core 的查询模型需要 Provider 才能适配具体数据库。

```csharp
options.UseNpgsql(connectionString);
```

Npgsql EF Core Provider 负责把 EF Core 操作翻译成 PostgreSQL Dialect，并通过底层 Npgsql Driver 通信。

如果改用 SQL Server，通常会换成另一个 Provider，但 Controller 和大量 LINQ 代码可能保持不变。

## Docker：提供可复现的运行环境

Docker 可以运行 PostgreSQL Image：

```text
postgres:18-alpine
```

它解决：

- 团队使用相同 PostgreSQL 版本；
- 不直接污染开发机；
- 可以通过 Compose 重建开发环境；
- 配置、端口、Volume 和 Healthcheck 可记录。

Docker 不是数据库 Driver，也不会替代 Npgsql。PostgreSQL 可以通过 Docker、系统安装或 Cloud Database 运行。

## Migration：记录 Schema Change

PostgreSQL 本身当然可以执行：

```sql
ALTER TABLE ...;
```

Migration 解决的是团队和多环境中的版本问题：

```text
这个字段何时加入？
测试环境执行到哪个版本？
生产环境还缺哪些 Schema Change？
代码回滚时结构如何处理？
```

EF Core Migration 把 Model Change 保存成可以检查和提交的 C# 文件，并通过 `__EFMigrationsHistory` 记录数据库已经应用的版本。

Migration 不等于 PostgreSQL Version Upgrade。`pg_upgrade` 处理 PostgreSQL Server 大版本升级；EF Core Migration 处理应用表结构变化。

## DbContext 位于哪里

`DbContext` 是应用使用 EF Core 的入口：

```csharp
public class AppDbContext : DbContext
{
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
}
```

Controller 或 Service 通过 DbContext 查询和保存 Entity。DbContext 再使用已配置的 Provider 与数据库通信。

## 职责对照

| 组件 | 核心职责 | 不负责什么 |
|---|---|---|
| PostgreSQL | 保存数据、执行 SQL 和 Constraint | 不理解 Controller 或 DTO |
| EF Core | ORM、LINQ、Tracking、Migration Model | 不启动数据库 Server |
| Npgsql | PostgreSQL Provider 和 Driver | 不保存业务数据 |
| Docker | 运行隔离、可复现的环境 | 不替代数据库或 Driver |
| Migration | 记录和应用 Schema Change | 不升级 PostgreSQL Server |

## 总结

```text
PostgreSQL → 数据在哪里保存
EF Core    → C# 如何表达数据操作
Npgsql     → EF Core 如何与 PostgreSQL 通信
Docker     → PostgreSQL 在什么环境运行
Migration  → 数据库结构如何随代码演进
```

先理解边界，再安装工具，可以避免把数据库连接失败误认为 NuGet Package 问题，或把 Docker 当作数据库 Driver。

## 参考资料

- [EF Core](https://learn.microsoft.com/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL](https://www.postgresql.org/docs/)
- [Docker Compose](https://docs.docker.com/compose/)

## 主线导航

- 上一步：[创建并运行 Controller API](./00-create-aspnet-core-controller-web-api.md)
- 下一步：[安装 EF Core PostgreSQL 工具链](./01a-install-ef-core-postgresql-tools.md)
