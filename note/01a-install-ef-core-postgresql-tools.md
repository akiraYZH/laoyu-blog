---
title: "为 ASP.NET Core 安装 EF Core PostgreSQL 工具链"
description: "安装 Npgsql Provider、EF Core Design Package 和项目级 dotnet-ef，并验证版本与职责。"
tags:
  - .NET 10
  - EF Core
  - Npgsql
  - dotnet-ef
  - NuGet
---

# 为 ASP.NET Core 安装 EF Core PostgreSQL 工具链

连接 PostgreSQL 和生成 Migration 需要不同类型的依赖。本文只完成 Package 与 CLI Tool 的安装和验证，不配置 Connection String，也不创建数据库表。

## 安装 Npgsql EF Core Provider

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

这个 Package 让 EF Core 支持：

```csharp
options.UseNpgsql(connectionString);
```

它会带入兼容的 EF Core 和 Npgsql 传递依赖，不需要为了“保险”重复添加所有底层 Package。

## 安装 EF Core Design Package

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

它提供 Migration 等 Design-time 功能。常见 `.csproj` 配置：

```xml
<PackageReference
  Include="Microsoft.EntityFrameworkCore.Design"
  Version="10.0.10">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>
    runtime; build; native; contentfiles; analyzers; buildtransitive
  </IncludeAssets>
</PackageReference>
```

`PrivateAssets=all` 表示引用当前项目的其他项目不会自动继承这个 Design-time Dependency。

## Package 不等于 CLI Tool

安装 `Microsoft.EntityFrameworkCore.Design` 后，下面命令仍可能不存在：

```bash
dotnet ef --version
```

原因是：

```text
PackageReference → 项目编译和 Design-time API
dotnet-ef Tool    → 终端中的 ef 命令
```

它们互相配合，但不是同一个安装对象。

## 创建 Local Tool Manifest

```bash
dotnet new tool-manifest
```

然后安装项目级工具：

```bash
dotnet tool install dotnet-ef
```

Local Tool 的版本记录在仓库中，团队成员和 CI 可以恢复相同工具链：

```bash
dotnet tool restore
```

相比 Global Tool，它更不容易出现不同项目要求不同版本的冲突。

## 验证安装

```bash
dotnet tool list --local
dotnet tool run dotnet-ef --version
dotnet list package
```

预期能够看到：

```text
dotnet-ef
Microsoft.EntityFrameworkCore.Design
Npgsql.EntityFrameworkCore.PostgreSQL
```

不要假定 Tool Manifest 一定放在某个固定子目录；以 `dotnet tool list --local` 的结果为准。

## 检查版本一致性

EF Core Runtime、Design Package、Provider 和 `dotnet-ef` 应使用兼容的 Major Version。

例如项目目标是 EF Core 10 时，不应随意混入 EF Core 8 的 Design Package。

```bash
dotnet list package --include-transitive
dotnet tool run dotnet-ef --version
```

版本警告应与数据库连接、路由或 Docker 错误分开处理。

## 安全与仓库边界

应该提交：

- `.csproj` 中的 PackageReference；
- Local Tool Manifest；
- Migration 文件。

不应该提交：

- 数据库密码；
- 本机绝对路径；
- 用户专属环境配置。

## 总结

```text
Npgsql Provider → 让 EF Core 支持 PostgreSQL
EF Core Design  → 提供 Design-time 能力
dotnet-ef       → 提供 CLI 命令
Local Manifest  → 固定并恢复工具版本
```

## 参考资料

- [EF Core tools reference](https://learn.microsoft.com/ef/core/cli/dotnet)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [.NET local tools](https://learn.microsoft.com/dotnet/core/tools/global-tools#install-a-local-tool)

## 主线导航

- 上一步：[理解数据库组件职责](./01-aspnet-core-postgresql-ef-core-setup.md)
- 下一步：[启动 PostgreSQL 开发数据库](./02-docker-compose-postgresql-development.md)
