---
title: "为 ASP.NET Core 博客 API 加入 Identity 管理员"
description: "使用 ASP.NET Core Identity、EF Core 和启动 Seeder 建立管理员账号，为后续 JWT 登录准备用户与角色数据。"
tags:
  - ASP.NET Core
  - Identity
  - EF Core
  - PostgreSQL
  - Dependency Injection
---

# 为 ASP.NET Core 博客 API 加入 Identity 管理员

博客的公开读取接口可以继续允许游客访问，但创建、修改、删除文章和上传图片需要管理员身份。要实现这个边界，项目首先需要可靠地保存用户、密码哈希和角色。

本文只完成 Identity 数据层和管理员初始化，不生成 JWT，也不保护 Controller。完成后，数据库中将存在一个拥有 `Admin` 角色的管理员，下一篇再实现登录。

## 前置状态

项目已经具有：

```text
ASP.NET Core Controller Web API
AppDbContext + PostgreSQL
EF Core Migration 工具链
BlogPostService 和文章 CRUD
```

## Identity 在项目中负责什么

Identity 负责：

```text
查找用户
生成和验证密码哈希
保存用户与角色
维护用户和角色的关系
```

JWT 不负责检查密码。后续登录时，Identity 先确认账号密码正确，JWT Service 才签发 Access Token。

## 安装 Identity EF Core 包

在项目根目录执行：

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.10
```

版本应与项目使用的 ASP.NET Core 和 EF Core 主版本保持一致。

## 创建用户模型

新建 `Models/ApplicationUser.cs`：

```csharp
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Models;

public sealed class ApplicationUser : IdentityUser
{
}
```

`IdentityUser` 已经包含 `Id`、`UserName`、`Email`、`PasswordHash` 等字段。当前不需要新增属性，但保留 `ApplicationUser` 可以让项目以后加入显示名称、头像等业务字段。

## 让 AppDbContext 管理 Identity 表

修改 `Data/AppDbContext.cs`：

```csharp
using BlogApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Data;

public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 保留项目已有的 BlogPost 和 Category 配置。
    }
}
```

关键变化是：

```csharp
IdentityDbContext<ApplicationUser>
```

它在原有 DbContext 基础上增加 Identity Entity。`base.OnModelCreating(modelBuilder)` 不能删除，否则 Identity 的表结构和关系不会正确配置。

## 注册 Identity 服务

在 `Program.cs` 中加入：

```csharp
using BlogApi.Models;
using Microsoft.AspNetCore.Identity;
```

在 `AddDbContext` 之后注册：

```csharp
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
```

这段代码完成三个连接：

```text
AddIdentityCore<ApplicationUser>
    → 注册 UserManager<ApplicationUser> 等用户服务

AddRoles<IdentityRole>
    → 注册 RoleManager<IdentityRole> 等角色服务

AddEntityFrameworkStores<AppDbContext>
    → 使用 AppDbContext 读写 Identity 数据表
```

Lambda 中的 `options` 是 `AddIdentityCore` 传入的配置对象，不是全局变量。它的名字可以改成 `identityOptions`。

## 配置管理员凭据

开发环境文件可以包含：

```dotenv
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=replace-with-a-development-password
```

Compose 把它们传给 ASP.NET Core：

```yaml
environment:
  Admin__Email: ${ADMIN_EMAIL}
  Admin__Password: ${ADMIN_PASSWORD}
```

双下划线会映射成配置路径：

```text
Admin__Email    → Admin:Email
Admin__Password → Admin:Password
```

不要把真实生产密码写入 Git。生产环境应通过部署平台的 Secret 管理能力提供这些值。

## 创建管理员 Seeder

新建 `Data/Seeding/AdminSeeder.cs`。它的职责是：

```text
确认 Admin 角色存在
确认管理员用户存在
确认用户拥有 Admin 角色
```

核心流程如下：

```csharp
using BlogApi.Models;
using Microsoft.AspNetCore.Identity;

namespace BlogApi.Data.Seeding;

public static class AdminSeeder
{
    private const string AdminRole = "Admin";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin email and password are not configured.");
        }

        using var scope = services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            EnsureSucceeded(
                await roleManager.CreateAsync(
                    new IdentityRole(AdminRole)),
                "Creating Admin role");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            EnsureSucceeded(
                await userManager.CreateAsync(admin, password),
                "Creating Admin user");
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(
                    admin,
                    AdminRole),
                "Assigning Admin role");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(
                error => error.Description));

        throw new InvalidOperationException(
            $"{operation} failed: {errors}");
    }
}
```

`UserManager` 和 `RoleManager` 不是手动 `new` 出来的。前面的 Identity 注册已经把它们加入 Dependency Injection Container，Seeder 通过 `GetRequiredService` 取得当前 Scope 中的实例。

## 启动时执行 Seeder

在 `Program.cs` 中，`builder.Build()` 之后、`app.Run()` 之前调用：

```csharp
var app = builder.Build();

await AdminSeeder.SeedAsync(
    app.Services,
    app.Configuration);
```

当前实现采用 Fail Fast：管理员配置缺失、密码不符合规则或创建角色失败时，`EnsureSucceeded` 会抛出异常，应用不会继续启动。这能避免部署后误以为管理员已经建立，但也意味着公共读取接口一起不可用。

生产项目应明确选择策略：

```text
认证是整个应用的必要能力
    → 初始化失败时终止启动

公共读取必须保持可用
    → 记录严重错误、跳过管理员初始化并继续启动
```

不要用一个空的 `catch` 静默吞掉数据库连接或 Schema 错误。

## 创建并应用 Migration

模型改变后创建 Migration：

```bash
make migration NAME=AddIdentity
make db-update
```

数据库将出现包括以下表在内的 Identity Schema：

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
AspNetUserClaims
AspNetRoleClaims
```

密码保存在 `PasswordHash` 中，不应保存或查询明文密码。

## 验证

先构建：

```bash
dotnet build
```

启动应用后确认：

1. API 成功启动；
2. `AspNetUsers` 中存在管理员；
3. `AspNetRoles` 中存在 `Admin`；
4. `AspNetUserRoles` 中存在两者的关联；
5. 再次启动不会重复创建用户或角色。

## 常见错误

### 密码格式错误导致应用退出

如果日志显示：

```text
Creating Admin user failed: Passwords must be at least ...
```

不是 Identity 自己突然 Crash。`CreateAsync` 返回了失败的 `IdentityResult`，当前 `EnsureSucceeded` 主动把它转换成未处理异常；Seeder 又在 `app.Run()` 之前执行，因此进程退出。

### 忘记调用 base.OnModelCreating

会导致 Identity 模型配置缺失。自定义 `OnModelCreating` 时必须首先调用：

```csharp
base.OnModelCreating(modelBuilder);
```

### 只安装包，没有创建 Migration

注册 Identity 不会自动修改数据库。只有生成并应用 Migration 后，Identity 表才真正存在。

## 完成状态

```text
ApplicationUser → 项目的用户 Entity
IdentityDbContext → 管理 Identity 数据表
UserManager → 用户查询、创建和密码验证
RoleManager → 角色查询和创建
AdminSeeder → 初始化管理员和 Admin 角色
```

## 参考资料

- [ASP.NET Core Identity 简介](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [ASP.NET Core Identity 配置](https://learn.microsoft.com/aspnet/core/security/authentication/identity-configuration)
- [ASP.NET Core 依赖注入](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)

## 主线导航

- 上一步：[为 Markdown 编辑器建立本地图片上传 API](./14c-build-local-image-upload-api.md)
- 下一步：[登录并签发 JWT Access Token](./15a-issue-jwt-access-token.md)
