---
title: "读懂 ASP.NET Core 项目的 csproj"
description: "理解 Microsoft.NET.Sdk.Web、TargetFramework、Nullable、ImplicitUsings 与 PackageReference 如何决定 .NET 项目的构建方式。"
tags:
  - .NET 10
  - csproj
  - MSBuild
  - NuGet
---

# 读懂 ASP.NET Core 项目的 csproj

`.csproj` 是 .NET 项目的构建说明书。CLI、IDE、NuGet 和 CI 都通过它判断项目如何编译以及依赖哪些 Package。

## 一个最小 Web API 项目文件

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

## `Microsoft.NET.Sdk.Web`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

它告诉 MSBuild：这是一个 ASP.NET Core Web 项目，而不是普通 Console 或 Class Library。

Web SDK 会引入 Web Hosting、Configuration、Logging 和 ASP.NET Core Shared Framework 等构建约定。

## `TargetFramework`

```xml
<TargetFramework>net10.0</TargetFramework>
```

表示项目使用 .NET 10 API Surface 和运行时目标。

本机安装了更新 SDK，不代表项目会自动升级 Target Framework。项目目标仍由 `.csproj` 决定。

## `Nullable`

```xml
<Nullable>enable</Nullable>
```

开启 Nullable Reference Type Analysis：

```csharp
public string Title { get; set; } = string.Empty;
public string? Summary { get; set; }
```

`string` 表示代码不希望它为 `null`；`string?` 表示允许 `null`。这主要是编译器分析，不会单独创建数据库约束或 API Validation。

## `ImplicitUsings`

```xml
<ImplicitUsings>enable</ImplicitUsings>
```

开启后，SDK 会自动加入一组常用 Namespace，因此某些文件不必重复写：

```csharp
using System;
using System.Collections.Generic;
```

它不会自动引入所有第三方 Package 的 Namespace。

## `PackageReference`

安装 NuGet Package 后会出现：

```xml
<ItemGroup>
  <PackageReference
    Include="Npgsql.EntityFrameworkCore.PostgreSQL"
    Version="10.0.3" />
</ItemGroup>
```

`Include` 是 Package ID，`Version` 固定项目使用的版本。

修改 Package 后运行：

```bash
dotnet restore
dotnet build
```

`restore` 下载并解析依赖，`build` 使用恢复结果编译源码。

## `.csproj` 与生成目录

```text
obj/ → Restore 和 Build 的中间文件
bin/ → 编译后的 DLL、配置和依赖
```

它们是生成结果，不应该代替 `.csproj` 提交依赖声明。

## 常用检查命令

```bash
dotnet list package
dotnet list package --include-transitive
dotnet list package --vulnerable --include-transitive
```

直接依赖写在 `.csproj` 中；传递依赖由直接 Package 带入，也应该定期检查安全信息。

## 总结

```text
Sdk              → 使用哪套构建约定
TargetFramework  → 面向哪个 .NET 版本
Nullable         → 是否检查引用类型 null 风险
ImplicitUsings   → 是否自动引入常用 Namespace
PackageReference → 项目依赖哪些 NuGet Package
```

## 参考资料

- [MSBuild Project SDK](https://learn.microsoft.com/visualstudio/msbuild/how-to-use-project-sdk)
- [Target frameworks](https://learn.microsoft.com/dotnet/standard/frameworks)
- [NuGet PackageReference](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files)

