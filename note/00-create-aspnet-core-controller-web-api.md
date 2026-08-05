---
title: "从零创建一个 .NET 10 ASP.NET Core Controller Web API"
description: "使用 macOS、VS Code 和 .NET CLI 创建可运行的 Controller Web API，理解项目模板、Program.cs、ControllerBase、属性路由、启动配置和请求测试。"
date: 2026-07-29
status: draft
tags:
  - ASP.NET Core
  - .NET 10
  - Web API
  - CSharp
  - VS Code
---

# 从零创建一个 .NET 10 ASP.NET Core Controller Web API

ASP.NET Core 可以用来开发网页、REST API、实时通信服务和后台任务。对于刚从前端转向 .NET 后端的开发者，最容易混淆的往往不是 C# 语法，而是项目模板：Minimal API、Controller Web API 和带 Razor View 的 MVC 项目看起来都叫 ASP.NET Core，但它们的代码组织和请求处理方式并不相同。

本文从一个空目录开始，使用 .NET 10、VS Code 和命令行创建一个基于 Controller 的 JSON Web API。完成后将得到两个可访问接口：

```http
GET /api/blogs
GET /api/blogs/1
```

第一版暂时使用内存列表，不接数据库。这样可以先理解 ASP.NET Core 的项目结构、依赖注入、请求管道和属性路由，再在后续文章中替换为 PostgreSQL。

## 为什么选择 Controller Web API

ASP.NET Core 中常见的三种写法可以简单区分为：

| 类型 | 典型代码 | 主要输出 | 适合场景 |
|---|---|---|---|
| Minimal API | `app.MapGet(...)` | JSON、文本等 | 小型服务、原型、轻量端点 |
| Controller Web API | `ControllerBase` | JSON API | 结构化 REST API、中大型后端 |
| Razor MVC | `Controller` + `View()` | 服务端 HTML | 由服务器渲染网页的应用 |

本文选择 Controller Web API，原因不是它“更高级”，而是它更适合按功能拆分较多接口：

- Controller 和 Action 的职责边界明确；
- 属性路由集中在对应类和方法上；
- 请求验证、过滤器和统一返回类型更容易组织；
- 后续加入 Service、数据库和身份认证时结构更清晰。

Controller Web API 的类通常继承：

```csharp
ControllerBase
```

而不是 Razor MVC 常用的：

```csharp
Controller
```

`ControllerBase` 提供 `Ok()`、`NotFound()`、`BadRequest()` 等 API 返回方法，但不包含 `View()` 页面渲染能力。

## 准备开发环境

本文使用：

- macOS；
- .NET 10 SDK；
- VS Code；
- C# Dev Kit 扩展；
- 可选的 REST Client 扩展。

先确认 .NET SDK 已正确安装：

```bash
dotnet --version
```

预期输出应以 `10.` 开头，例如：

```text
10.0.302
```

需要查看更完整的 SDK 和运行时信息时，可以运行：

```bash
dotnet --info
```

如果终端显示：

```text
command not found: dotnet
```

这说明 .NET SDK 尚未安装或没有加入 `PATH`。它不是 ASP.NET Core 项目本身的错误，应先修复开发环境。

VS Code 建议安装：

```text
C# Dev Kit
扩展 ID：ms-dotnettools.csdevkit
```

如果希望直接在 `.http` 文件中发送请求，还可以安装：

```text
REST Client
扩展 ID：humao.rest-client
```

## 使用 CLI 创建项目

选择一个保存代码的目录，然后运行：

```bash
dotnet new webapi \
  -n BlogApi \
  -f net10.0 \
  --use-controllers
```

进入项目目录：

```bash
cd BlogApi
```

这条创建命令可以拆成四部分理解。

### `dotnet new webapi`

```bash
dotnet new webapi
```

`dotnet new` 用于根据模板创建项目，`webapi` 表示使用 ASP.NET Core Web API 模板。

它类似前端生态中的脚手架命令：

```bash
npm create vite
```

模板负责创建基础目录、项目文件、示例端点和启动配置。

### `-n BlogApi`

```bash
-n BlogApi
```

`-n` 是 `--name` 的缩写，用来指定项目名称。默认情况下，它也会创建名为 `BlogApi` 的目录，并影响程序集名称和默认命名空间。

### `-f net10.0`

```bash
-f net10.0
```

`-f` 是 `--framework` 的缩写，表示项目目标框架是 .NET 10。

生成的 `.csproj` 会包含：

```xml
<TargetFramework>net10.0</TargetFramework>
```

### `--use-controllers`

```bash
--use-controllers
```

这个参数要求 Web API 模板使用 Controller，而不是模板默认倾向的 Minimal API 组织方式。

它只影响创建项目时生成的初始结构，不会把项目永久锁死。即使创建时没有使用该参数，后面仍然可以手动加入 `AddControllers()`、`MapControllers()` 和 Controller 类。

Microsoft 的 Controller Web API 教程也使用 `dotnet new webapi --use-controllers` 创建项目。[ASP.NET Core 官方教程](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api)

## 理解生成的项目结构

不同 SDK 补丁版本生成的示例文件可能略有变化，但核心结构通常类似：

```text
BlogApi/
├── Controllers/
│   └── WeatherForecastController.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── BlogApi.csproj
├── BlogApi.http
├── Program.cs
└── WeatherForecast.cs
```

这些文件并不是同一个层级的功能。

| 文件 | 作用 |
|---|---|
| `BlogApi.csproj` | 项目类型、目标框架和 NuGet 依赖 |
| `Program.cs` | 注册服务并配置 HTTP 请求管道 |
| `Controllers/` | 放置 Controller API 类 |
| `launchSettings.json` | 本地开发启动 Profile、端口和环境变量 |
| `appsettings.json` | 通用应用配置 |
| `appsettings.Development.json` | Development 环境覆盖配置 |
| `BlogApi.http` | 用于手动发送 HTTP 请求的文本文件 |

## 理解 `.csproj`

Web API 项目文件的基础结构类似：

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference
      Include="Microsoft.AspNetCore.OpenApi"
      Version="10.0.10" />
  </ItemGroup>
</Project>
```

### `Microsoft.NET.Sdk.Web`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

它告诉 .NET SDK：这是一个 Web 项目，需要 ASP.NET Core 的构建和发布能力。

### `TargetFramework`

```xml
<TargetFramework>net10.0</TargetFramework>
```

表示代码以 .NET 10 为目标框架。

### `Nullable`

```xml
<Nullable>enable</Nullable>
```

启用可空引用类型分析。编译器会帮助发现可能为 `null` 的引用，减少运行时的空引用异常。

### `ImplicitUsings`

```xml
<ImplicitUsings>enable</ImplicitUsings>
```

自动引入 Web 项目常用的命名空间，因此部分文件不需要重复书写所有 `using`。

### `PackageReference`

```xml
<PackageReference Include="..." Version="..." />
```

它表示 NuGet 依赖。可以把 NuGet 类比为 .NET 生态中的 npm，而 `.csproj` 同时承担一部分 `package.json` 和构建配置的职责。

## 理解 Program.cs

Controller Web API 的 `Program.cs` 通常包含：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

代码可以分成三个阶段。

### 第一阶段：创建 Builder

```csharp
var builder = WebApplication.CreateBuilder(args);
```

Builder 负责收集应用启动所需的信息，包括：

- 配置文件；
- 环境变量；
- 日志；
- 依赖注入服务；
- 命令行参数。

### 第二阶段：注册服务

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi();
```

`builder.Services` 是依赖注入容器的注册入口。

`AddControllers()` 注册 Controller API 所需服务，包括 Controller 发现、模型绑定、数据验证和 JSON 格式化。

`AddOpenApi()` 注册 OpenAPI 文档生成服务，但这里只是“把能力加入依赖注入容器”，还没有暴露 HTTP 端点。

### 第三阶段：构建应用并配置管道

```csharp
var app = builder.Build();
```

这一行根据前面收集的服务和配置创建可运行的 WebApplication。

随后配置请求经过的处理步骤：

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

只有在 Development 环境中才映射 OpenAPI JSON 端点。这样可以避免无意中在生产环境暴露开发文档。

```csharp
app.UseHttpsRedirection();
```

当应用同时配置 HTTP 和 HTTPS 时，把 HTTP 请求重定向到 HTTPS。

```csharp
app.UseAuthorization();
```

把授权中间件加入请求管道。它不等于“已经完成登录系统”；真正的身份认证和授权策略还需要后续配置。

```csharp
app.MapControllers();
```

把通过属性定义的 Controller 路由映射为 HTTP 端点。只有 `AddControllers()` 而没有 `MapControllers()`，Controller 仍然不会收到请求。

```csharp
app.Run();
```

启动 Web Server，并持续监听请求。

整个关系可以概括为：

```text
AddControllers()
    → 注册 Controller 所需服务

MapControllers()
    → 把 Controller 路由变成可访问端点
```

## 创建第一个 Blog Controller

在 `Controllers` 目录中新建 `BlogsController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers;

public record Blog(int Id, string Title);

[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    private static readonly List<Blog> Blogs =
    [
        new(1, "Hello ASP.NET Core"),
        new(2, "Hello PostgreSQL")
    ];

    [HttpGet]
    public ActionResult<IEnumerable<Blog>> GetAll()
    {
        return Ok(Blogs);
    }

    [HttpGet("{id:int}")]
    public ActionResult<Blog> GetById(int id)
    {
        var blog = Blogs.FirstOrDefault(blog => blog.Id == id);

        return blog is null ? NotFound() : Ok(blog);
    }
}
```

这个 Controller 暂时没有数据库，重点是观察路由和响应。

## 属性如何决定 URL

Controller 上的：

```csharp
[Route("api/[controller]")]
```

其中 `[controller]` 会取 Controller 类名并移除 `Controller` 后缀：

```text
BlogsController
    ↓
Blogs
    ↓
/api/blogs
```

方法上的：

```csharp
[HttpGet]
```

表示匹配 Controller 基础路径上的 GET 请求：

```http
GET /api/blogs
```

另一个方法使用：

```csharp
[HttpGet("{id:int}")]
```

它在基础路径后增加一个必须是整数的参数：

```http
GET /api/blogs/1
```

请求中的 `1` 会绑定到方法参数：

```csharp
GetById(int id)
```

`[ApiController]` 会启用适合 API 的约定行为，例如自动模型验证响应和参数绑定推断。

## 理解 ActionResult 和状态码

列表接口返回：

```csharp
return Ok(Blogs);
```

`Ok(...)` 生成：

```text
HTTP 200 OK
Content-Type: application/json
```

按 ID 查询时：

```csharp
return blog is null ? NotFound() : Ok(blog);
```

可能得到两种结果：

```text
找到数据     → 200 OK
找不到数据   → 404 Not Found
```

使用：

```csharp
ActionResult<Blog>
```

意味着 Action 既可以返回一个 `Blog`，也可以返回 `NotFound()` 等 HTTP 结果。

## 启动 API

先恢复 NuGet 依赖并编译：

```bash
dotnet restore
dotnet build
```

然后使用 HTTP Profile 启动：

```bash
dotnet run --launch-profile http
```

开发期间希望修改文件后自动重新编译，可以使用：

```bash
dotnet watch run --launch-profile http
```

`dotnet run` 只负责本次启动；`dotnet watch run` 会监控文件变化，并在需要时重新构建或重启应用。[dotnet watch 官方文档](https://learn.microsoft.com/aspnet/core/tutorials/dotnet-watch)

启动成功后，不要先猜端口。应观察终端输出：

```text
Now listening on: http://localhost:xxxx
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
```

其中 `xxxx` 是模板为本地 Profile 配置的端口。

## launchSettings.json 管理什么

本地启动 Profile 位于：

```text
Properties/launchSettings.json
```

结构类似：

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7000;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

端口不保证和示例相同，应该读取自己项目中的 `applicationUrl`。

需要注意：

- `launchSettings.json` 主要服务于本地开发；
- `--launch-profile http` 选择 `http` Profile；
- `ASPNETCORE_ENVIRONMENT=Development` 会启用 Development 配置；
- Docker 部署不会自动依赖 Visual Studio/VS Code 的本地启动 Profile；
- `Development` 是运行环境，`Debug`/`Release` 是构建配置，两者不是同一个概念。

## 测试接口

假设终端显示：

```text
Now listening on: http://localhost:5000
```

可以使用 curl：

```bash
curl http://localhost:5000/api/blogs
```

查询单篇博客：

```bash
curl http://localhost:5000/api/blogs/1
```

查询不存在的 ID：

```bash
curl -i http://localhost:5000/api/blogs/999
```

预期最后一个请求返回：

```text
HTTP/1.1 404 Not Found
```

## 使用 .http 文件发送请求

也可以在项目根目录创建 `BlogApi.http`：

```http
@host = http://localhost:5000

GET {{host}}/api/blogs
Accept: application/json

###

GET {{host}}/api/blogs/1
Accept: application/json

###

GET {{host}}/api/blogs/999
Accept: application/json
```

其中：

- `@host` 定义变量；
- `{{host}}` 使用变量；
- `###` 分隔多个请求；
- `Accept: application/json` 表示客户端希望接收 JSON。

在 VS Code 安装 REST Client 后，可以点击请求上方的 `Send Request`，或者把光标放在请求中并使用快捷键：

```text
Cmd + Alt + R
```

如果实际端口不是 `5000`，必须修改 `.http` 文件中的 `@host`。

## 为什么访问根路径会返回 404

创建 Controller 后，很多人会先打开：

```text
http://localhost:5000/
```

然后因为看到 404 就判断“项目启动失败”。这个结论通常不正确。

当前代码只映射了：

```text
GET /api/blogs
GET /api/blogs/{id}
```

并没有定义：

```text
GET /
```

因此根路径 404 代表路由不存在，不代表服务器没有启动。

诊断顺序应该是：

1. 终端是否出现 `Now listening on`；
2. 当前启动端口是什么；
3. Controller 的 `[Route]` 是什么；
4. Action 的 `[HttpGet]`、`[HttpPost]` 等属性是什么；
5. 客户端是否请求了正确的 HTTP Method 和 URL。

如果确实需要根路径，可以主动映射：

```csharp
app.MapGet("/", () => Results.Ok(new
{
    message = "Blog API is running"
}));
```

这不会把项目变成 Minimal API 项目。ASP.NET Core 可以同时使用 Controller 路由和少量 Minimal API 端点。

## HTTP Profile 下的 HTTPS 重定向警告

如果使用只包含 HTTP 地址的 Profile，同时保留：

```csharp
app.UseHttpsRedirection();
```

开发环境中可能出现无法确定 HTTPS 端口的警告。它和“服务器是否启动成功”是两个问题。

仍然应该先观察：

```text
Now listening on: http://localhost:xxxx
```

如果需要完整测试 HTTPS，可以信任本地开发证书：

```bash
dotnet dev-certs https --trust
```

然后运行 HTTPS Profile：

```bash
dotnet run --launch-profile https
```

## OpenAPI 端点不等于 Swagger UI

模板中的：

```csharp
builder.Services.AddOpenApi();
```

和：

```csharp
app.MapOpenApi();
```

会生成并暴露 OpenAPI 文档端点，但不应自动假设项目一定包含可视化 Swagger UI。OpenAPI 文档和 Swagger UI 是相关但不同的组件。

另外，示例代码把 `MapOpenApi()` 放在：

```csharp
if (app.Environment.IsDevelopment())
```

内部，所以 Production 环境默认不会映射该端点。

## 一次请求是如何到达 Controller 的

以：

```http
GET /api/blogs/1
```

为例，完整过程可以简化为：

```text
客户端发送 HTTP 请求
    ↓
ASP.NET Core Server 接收请求
    ↓
请求经过中间件管道
    ↓
MapControllers 查找匹配端点
    ↓
[Route("api/[controller]")]
+ [HttpGet("{id:int}")]
    ↓
执行 BlogsController.GetById(1)
    ↓
Ok(blog) 或 NotFound()
    ↓
ASP.NET Core 序列化为 HTTP 响应
```

这条链路也是以后排查 API 问题的基本框架：

```text
启动 → 端口 → 路由 → 模型绑定 → Action → 业务逻辑 → 响应
```

## 从零重建检查清单

以后需要重新创建同类项目时，可以按以下顺序操作：

```bash
# 1. 确认 SDK
dotnet --version

# 2. 创建 Controller Web API
dotnet new webapi -n BlogApi -f net10.0 --use-controllers

# 3. 进入项目
cd BlogApi

# 4. 恢复依赖
dotnet restore

# 5. 编译
dotnet build

# 6. 启动开发服务
dotnet watch run --launch-profile http
```

然后：

1. 从 `Now listening on` 读取真实端口；
2. 阅读 `Properties/launchSettings.json`；
3. 创建继承 `ControllerBase` 的 Controller；
4. 添加 `[ApiController]` 和 `[Route]`；
5. 用 `[HttpGet]` 等属性定义 Action；
6. 用 curl 或 `.http` 文件请求真实路由；
7. 不要用根路径 404 判断应用是否启动失败。

## 总结

创建一个 ASP.NET Core Controller Web API 的关键命令只有一条：

```bash
dotnet new webapi -n BlogApi -f net10.0 --use-controllers
```

但真正需要理解的是这条命令生成的运行模型：

- `.csproj` 定义项目类型、目标框架和依赖；
- `AddControllers()` 注册 Controller 服务；
- `MapControllers()` 映射属性路由；
- `ControllerBase` 提供适合 JSON API 的返回能力；
- `[Route]` 和 `[HttpGet]` 共同决定请求 URL；
- `launchSettings.json` 控制本地 Profile、端口和环境；
- `dotnet watch run` 提供适合开发期的自动重载；
- 404 只表示请求的路由不存在，不等于服务器启动失败。

掌握这套基础后，再加入 PostgreSQL、EF Core、身份认证和 Docker，就不会把路由、运行环境、数据库连接和业务逻辑混成同一个问题。

## 参考资料

- [Create a web API with ASP.NET Core controllers](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api)
- [Create web APIs with ASP.NET Core](https://learn.microsoft.com/aspnet/core/web-api/)
- [Routing to controller actions](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing)
- [dotnet watch](https://learn.microsoft.com/aspnet/core/tutorials/dotnet-watch)
- [ASP.NET Core environments](https://learn.microsoft.com/aspnet/core/fundamentals/environments)

