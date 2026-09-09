---
title: "使用 ASP.NET Core Identity 登录并签发 JWT"
description: "验证管理员邮箱与密码，把用户 ID、邮箱和角色写入 Claims，并返回带签名和过期时间的 JWT Access Token。"
tags:
  - ASP.NET Core
  - Identity
  - JWT
  - Claims
  - Authentication
---

# 使用 ASP.NET Core Identity 登录并签发 JWT

数据库已经存在管理员和 `Admin` 角色，但客户端还没有办法证明自己已经登录。本文增加一个登录接口：Identity 验证邮箱与密码，验证成功后由 JWT Service 签发 Access Token。

本文只负责生成 Token。下一篇再配置 Bearer 验证和 `[Authorize]`。

## 登录和 JWT 的职责边界

```text
Identity → 确认邮箱和密码是否正确
JWT      → 登录成功后生成防篡改的通行证
```

JWT 不会再次保存密码，也不会把密码写入 Token。

## 安装 JWT Bearer 包

在项目根目录执行：

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.10
```

## 创建强类型 JWT 配置

新建 `Options/JwtOptions.cs`：

```csharp
namespace BlogApi.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}
```

这个类型不是 Framework 自动提供的，而是项目自己的配置模型：

```text
Issuer            → 谁签发 Token
Audience          → Token 准备交给哪个系统使用
Key               → 生成和验证签名的秘密
ExpirationMinutes → Token 有效时间
```

名称表达 JWT 的标准概念，但具体值由项目决定。开发环境示例：

```dotenv
JWT_ISSUER=blog-api
JWT_AUDIENCE=blog-admin-frontend
JWT_KEY=replace-with-at-least-32-bytes-of-random-data
JWT_EXPIRATION_MINUTES=60
```

Compose 映射为 ASP.NET Core 配置：

```yaml
environment:
  Jwt__Issuer: ${JWT_ISSUER}
  Jwt__Audience: ${JWT_AUDIENCE}
  Jwt__Key: ${JWT_KEY}
  Jwt__ExpirationMinutes: ${JWT_EXPIRATION_MINUTES}
```

`Key` 必须保留在服务端，不得返回给前端或提交到 Git。

## 读取并验证配置

在 `Program.cs` 中：

```csharp
using System.Text;
using BlogApi.Options;
```

读取配置：

```csharp
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32
    || jwtOptions.ExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT configuration is invalid.");
}
```

强类型对象让后续代码使用：

```csharp
jwtOptions.Issuer
jwtOptions.Audience
jwtOptions.Key
jwtOptions.ExpirationMinutes
```

而不是在多个文件中重复读取容易拼错的配置字符串。

## 创建 Token Service 的内部返回值

新建 `Services/Auth/JwtTokenResult.cs`：

```csharp
namespace BlogApi.Services.Auth;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
```

这是 Service 内部 Contract。后面还会创建单独的 `LoginResponseDto` 作为 HTTP Response Contract，两者职责不同。

## 创建 JwtTokenService

新建 `Services/Auth/JwtTokenService.cs`：

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogApi.Models;
using BlogApi.Options;
using Microsoft.IdentityModel.Tokens;

namespace BlogApi.Services.Auth;

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public JwtTokenResult CreateToken(
        ApplicationUser user,
        IList<string> roles)
    {
        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(
            _options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty),
            new(
                JwtRegisteredClaimNames.UniqueName,
                user.UserName ?? user.Email ?? user.Id),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        claims.AddRange(
            roles.Select(
                role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Key));

        var signingConfig = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: signingConfig);

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(
            accessToken,
            expiresAtUtc);
    }
}
```

## 读懂 Claims

Claim 是关于当前用户的一条 `Type → Value` 信息：

```text
sub         → 用户 ID
email       → 用户邮箱
unique_name → 用户名
jti         → 当前 Token 的唯一编号
role        → 用户角色
```

下面的 LINQ：

```csharp
roles.Select(role => new Claim("role", role))
```

与 JavaScript 的 `roles.map(...)` 类似：它把每个角色字符串转换成一个 Claim。`AddRange` 再把生成的全部 Claim 加入列表。等价写法是：

```csharp
foreach (var role in roles)
{
    claims.Add(new Claim("role", role));
}
```

## 读懂签名代码

```csharp
var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(_options.Key));
```

先把配置中的字符串 Key 转成 Bytes，再包装成 JWT Library 能使用的对称密钥对象。

```csharp
var signingConfig = new SigningCredentials(
    signingKey,
    SecurityAlgorithms.HmacSha256);
```

这里只是把“使用哪个 Key”和“使用 HMAC SHA-256 算法”组合成签名配置。`JwtSecurityToken` 创建 Token 时才使用它完成签名。

签名防止伪造和篡改，但 Payload 只是编码，不是加密。不要把密码、Connection String 或其他 Secret 放入 Claims。

## 注册配置和 Service

在 `Program.cs` 中：

```csharp
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();
```

Dependency Injection 的连接过程是：

```text
Program.cs 注册 jwtOptions
    ↓
创建 JwtTokenService 时自动传入 JwtOptions
    ↓
创建 AuthController 时自动传入 JwtTokenService
```

`JwtOptions` 和 `JwtTokenService` 都不包含每个 Request 独有的可变状态，因此当前项目把它们注册为 Singleton。

## 创建登录 DTO

新建 `Dtos/LoginDto.cs`：

```csharp
using System.ComponentModel.DataAnnotations;

namespace BlogApi.Dtos.Auth;

public sealed class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

新建 `Dtos/LoginResponseDto.cs`：

```csharp
namespace BlogApi.Dtos.Auth;

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc);
```

Request DTO 接收账号密码，Response DTO 只返回 Token 元数据，不返回用户 Entity 或密码字段。

## 创建登录 Controller

新建 `Controllers/AuthController.cs`：

```csharp
using BlogApi.Dtos.Auth;
using BlogApi.Models;
using BlogApi.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        if (user is null
            || !await _userManager.CheckPasswordAsync(
                user,
                dto.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.CreateToken(user, roles);

        return Ok(new LoginResponseDto(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc));
    }
}
```

`_userManager` 的来源是 Identity 注册。ASP.NET Core 创建 Controller 时，从 Dependency Injection Container 中取得 `UserManager<ApplicationUser>` 并传给构造函数。

## 完整调用链

```text
POST /api/auth/login
    ↓
LoginDto 绑定和验证 JSON
    ↓
UserManager 查找用户、检查密码哈希
    ↓
UserManager 读取角色
    ↓
JwtTokenService 生成 Claims、过期时间和签名
    ↓
LoginResponseDto 返回 Access Token
```

## 验证

发送：

```http
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "replace-with-development-password"
}
```

成功时：

```http
200 OK
```

```json
{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2030-01-01T00:00:00Z"
}
```

账号不存在或密码错误时返回 `401 Unauthorized`。错误信息统一为 `Invalid email or password.`，避免向外部用户泄露某个邮箱是否已经存在。

## 常见错误

### JSON 中混入未转义换行

错误中出现：

```text
'0x0A' is invalid within a JSON string
```

表示 Request Body 的字符串中混入了换行符。请求尚未进入 `Login()`，`[ApiController]` 会在 Model Binding 阶段返回 400。删除 Body 并使用标准英文双引号重新输入 JSON。

### 把 `using` 当成 Node 的 Named Import

C# 的：

```csharp
using Microsoft.IdentityModel.Tokens;
```

只是允许省略 Namespace。完整类型名仍然是：

```csharp
Microsoft.IdentityModel.Tokens.TokenValidationParameters
Microsoft.IdentityModel.Tokens.SymmetricSecurityKey
```

`.csproj` 的 Package Reference 决定项目拥有哪些 Library；`using` 只让类型名称变短。

### 认为拿到 Token 就永远安全

Bearer 表示持有者。任何拿到有效 Token 的人都可以在过期前使用它，所以必须使用 HTTPS，避免把 Token 放进 URL、日志或源码，并控制过期时间。

## 完成状态

```text
Identity → 验证管理员账号密码
Claims → 描述用户 ID、邮箱和角色
JwtTokenService → 生成并签名 Access Token
AuthController → 暴露匿名登录接口
```

此时 Token 已能生成，但写接口还没有验证它。

## 参考资料

- [配置 JWT Bearer Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [Claim-based Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/claims)

## 主线导航

- 上一步：[为 ASP.NET Core 博客 API 加入 Identity 管理员](./15-add-aspnet-core-identity-admin.md)
- 下一步：[使用 JWT Bearer 保护博客写接口](./15b-protect-blog-write-endpoints-jwt.md)
