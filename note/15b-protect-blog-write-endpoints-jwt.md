---
title: "使用 JWT Bearer 保护 ASP.NET Core 博客写接口"
description: "配置 JWT Bearer 验证和角色授权，让游客继续读取文章，仅允许 Admin 创建、修改、删除文章和上传图片。"
tags:
  - ASP.NET Core
  - JWT Bearer
  - Authentication
  - Authorization
  - Roles
---

# 使用 JWT Bearer 保护 ASP.NET Core 博客写接口

登录接口已经能够返回 JWT，但仅仅生成 Token 不会自动保护任何接口。项目还需要配置服务器如何验证 Token，并明确哪些 Controller Action 要求 `Admin` 角色。

本文完成以下权限边界：

```text
游客 → 可以读取文章和分类
Admin → 可以创建、修改、删除文章和上传图片
```

## Authentication 和 Authorization

两个概念不能混在一起：

```text
Authentication → 验证 Token，确认用户是谁
Authorization  → 根据角色或规则，判断用户能做什么
```

对应到 ASP.NET Core：

```text
AddJwtBearer + UseAuthentication → Authentication
AddAuthorization + [Authorize]   → Authorization
```

## 配置 JWT Bearer 验证

在 `Program.cs` 引入：

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
```

注册 Authentication：

```csharp
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Key)),

                NameClaimType =
                    JwtRegisteredClaimNames.UniqueName,

                RoleClaimType = "role",

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();
```

`JwtBearerDefaults.AuthenticationScheme` 是 Library 提供的默认 Scheme 名称，值是 `Bearer`。它告诉 Authentication 系统从下面的 Header 读取 Token：

```http
Authorization: Bearer eyJ...
```

## TokenValidationParameters 检查什么

`TokenValidationParameters` 来自：

```csharp
Microsoft.IdentityModel.Tokens
```

它不是项目自己的变量，而是描述 JWT 验证规则的 Class：

| 配置 | 检查内容 |
|---|---|
| `ValidateIssuer` | Token 是否由预期系统签发 |
| `ValidateAudience` | Token 是否准备交给当前系统使用 |
| `ValidateLifetime` | Token 是否已生效且尚未过期 |
| `ValidateIssuerSigningKey` | 签名是否有效，内容是否被篡改 |

生成 Token 时，`JwtTokenService` 写入：

```csharp
issuer: jwtOptions.Issuer
audience: jwtOptions.Audience
```

验证时分别与：

```csharp
ValidIssuer
ValidAudience
```

比较。`audience` 最终成为 JWT 的 `aud` Claim，可以理解为“这个 Token 是发给谁使用的”。它同样受到签名保护，不能在不破坏签名的情况下直接修改。

签发和验证必须使用相同的 Secret Key。验证端用 Key 重新计算签名；结果不同就拒绝 Token。

## RoleClaimType 为什么是 role

Token Service 使用：

```csharp
new Claim("role", "Admin")
```

验证配置使用：

```csharp
RoleClaimType = "role"
```

两端名称必须对应。这样 ASP.NET Core 才知道 JWT 中的：

```text
role = Admin
```

应该用于：

```csharp
User.IsInRole("Admin")
```

和：

```csharp
[Authorize(Roles = "Admin")]
```

`MapInboundClaims = false` 让 Claim 名称保持 JWT 中的原始写法，避免 Framework 自动转换名称。

## 把 Middleware 加入请求 Pipeline

在 `Program.cs` 中，映射 Controller 之前加入：

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

顺序表达实际因果关系：

```text
UseAuthentication
    → 读取并验证 Bearer Token
    → 建立 HttpContext.User

UseAuthorization
    → 读取 [Authorize] 要求
    → 检查当前 User 是否满足要求

MapControllers
    → 执行符合权限的 Action
```

必须先知道用户是谁，才能判断其是否有权限。

## 保护文章写接口

在 `BlogsController.cs` 引入：

```csharp
using Microsoft.AspNetCore.Authorization;
```

只在 POST、PUT 和 DELETE 上加入：

```csharp
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<ActionResult<BlogPostResponseDto>>
    CreatePost(BlogPostDto dto)
{
    // 原有创建逻辑
}

[Authorize(Roles = "Admin")]
[HttpPut("{id:int}")]
public async Task<ActionResult<BlogPostResponseDto>>
    UpdatePost(int id, BlogPostDto dto)
{
    // 原有更新逻辑
}

[Authorize(Roles = "Admin")]
[HttpDelete("{id:int}")]
public async Task<ActionResult> DeletePost(int id)
{
    // 原有删除逻辑
}
```

GET Action 不添加 `[Authorize]`，因此游客仍然可以阅读。

## 保护图片上传

如果整个 `UploadsController` 都只供管理员使用，可以把 Attribute 放在 Class 上：

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api")]
public sealed class UploadsController : ControllerBase
{
    // 原有上传逻辑
}
```

Class 级别 Attribute 会应用到 Controller 中的所有 Action。

## 完整请求流程

```text
POST /api/auth/login
    ↓
Identity 验证邮箱和密码
    ↓
JwtTokenService 返回带 role=Admin 的 Token
    ↓
客户端发送 Authorization: Bearer <token>
    ↓
UseAuthentication 验证签名、Issuer、Audience 和过期时间
    ↓
Claims 被放入 HttpContext.User
    ↓
UseAuthorization 检查 [Authorize(Roles = "Admin")]
    ↓
符合要求后执行 Controller Action
```

## 验证

### 1. 登录并复制 Token

```http
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "replace-with-development-password"
}
```

从 `accessToken` 复制完整 Token。

### 2. 不带 Token 创建文章

```http
POST http://localhost:8080/api/blogs
Content-Type: application/json

{
  "title": "Protected Post",
  "slug": "protected-post",
  "content": "Authentication test."
}
```

预期：

```http
401 Unauthorized
```

### 3. 带 Admin Token 创建文章

```http
POST http://localhost:8080/api/blogs
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "title": "Protected Post",
  "slug": "protected-post",
  "content": "Authentication test."
}
```

预期：

```http
201 Created
```

### 4. 游客读取文章

```http
GET http://localhost:8080/api/blogs?page=1&pageSize=10
```

不带 Token 仍应返回 `200 OK`。

## 401 和 403 的区别

```text
401 Unauthorized
    → 没有 Token、Token 无效或 Token 已过期

403 Forbidden
    → Token 有效，已经知道用户是谁
    → 但用户没有 Admin 角色
```

## 前端拿到 Token 后做什么

当前 Bearer 方案要求客户端在管理请求中加入：

```http
Authorization: Bearer <access-token>
```

学习项目可以先把 Token 保存在 Pinia，并按需要同步到 `sessionStorage`。退出登录时清除它。Route Guard 只能改善前端体验，真正的安全边界仍然是后端 `[Authorize]`。

生产环境还需要评估 `HttpOnly` Cookie、Refresh Token、撤销策略和 CSRF/XSS 风险，不能因为已经使用 JWT 就认为认证设计自动安全。

## 常见错误

### 登录成功，但写接口不需要 Token

只实现 `JwtTokenService` 不会自动保护 API。还必须：

```text
AddAuthentication().AddJwtBearer(...)
UseAuthentication()
UseAuthorization()
[Authorize]
```

### Token 有 role，但角色授权失败

确认生成端和验证端使用相同 Claim Type：

```csharp
new Claim("role", role)
RoleClaimType = "role"
```

### 把 Bearer Token 当成防盗机制

JWT 签名防止伪造和篡改，但任何拿到有效 Token 的人都能在过期前使用它。必须使用 HTTPS，不要把 Token 放入 URL、公开日志或 Git。

## 完成状态

```text
公开 GET → 游客可访问
POST、PUT、DELETE → 需要 Admin Token
图片上传 → 需要 Admin Token
UseAuthentication → 建立用户身份
UseAuthorization → 执行角色权限判断
```

## 参考资料

- [配置 JWT Bearer Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [ASP.NET Core Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/introduction)
- [Role-based Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/roles)

## 主线导航

- 上一步：[登录并签发 JWT Access Token](./15a-issue-jwt-access-token.md)
