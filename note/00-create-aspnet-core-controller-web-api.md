---
title: "从零创建并运行 ASP.NET Core Controller Web API"
description: "使用 .NET 10 CLI 创建最小 Controller Web API，添加第一个接口，并完成编译、启动和 HTTP 验证。"
tags:
  - .NET 10
  - ASP.NET Core
  - Web API
  - Controller
---

# 从零创建并运行 ASP.NET Core Controller Web API

本文只解决一个问题：在 macOS、Linux 或 Windows 上，用 .NET CLI 创建并跑通一个 Controller Web API。

## 检查 .NET SDK

```bash
dotnet --version
```

输出 `10.x` 表示 .NET 10 SDK 可用。SDK 包含创建、恢复依赖、编译和运行项目所需的 CLI。

如果命令不存在，需要先安装 SDK，而不是只安装 ASP.NET Runtime。Runtime 只能运行已经构建好的程序，不能创建和编译项目。

## 创建 Controller Web API

```bash
dotnet new webapi \
  -n BlogApi \
  -f net10.0 \
  --use-controllers
```

参数职责：

| 参数 | 含义 |
|---|---|
| `webapi` | 使用 ASP.NET Core Web API Template |
| `-n BlogApi` | 创建名为 `BlogApi` 的项目目录和项目文件 |
| `-f net10.0` | Target Framework 使用 .NET 10 |
| `--use-controllers` | 使用 Controller，而不是默认的 Minimal API 结构 |

进入项目：

```bash
cd BlogApi
```

## 创建第一个 Controller

新建 `Controllers/BlogsController.cs`：

```csharp
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<string>> GetPosts()
    {
        return Ok(new[] { "First post", "Second post" });
    }

    [HttpGet("{id:int}")]
    public ActionResult<string> GetPost(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        return Ok($"Post {id}");
    }
}
```

模板通常已经在 `Program.cs` 注册并映射 Controller：

```csharp
builder.Services.AddControllers();

// ...

app.MapControllers();
```

前者把 Controller 支持加入 Dependency Injection，后者把 Attribute Route 加入 HTTP Pipeline。两行缺少任何一行，Controller 都无法正常处理请求。

## 编译项目

```bash
dotnet build
```

预期结果：

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Build 可以提前发现 C# 语法、类型和 Package 问题，但它不会启动 HTTP Server。

## 启动 API

```bash
dotnet run
```

终端会显示实际监听地址：

```text
Now listening on: http://localhost:5000
```

端口可能不同，应以终端输出为准。

## 验证接口

```bash
curl -i http://localhost:5000/api/blogs
```

预期：

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
["First post", "Second post"]
```

测试带 ID 的路由：

```bash
curl -i http://localhost:5000/api/blogs/1
curl -i http://localhost:5000/api/blogs/0
```

预期分别返回 `200 OK` 和 `404 Not Found`。

## 完成标准

以下结果全部成立，才算真正跑通：

- `dotnet build` 成功；
- 日志出现 `Now listening on`；
- `GET /api/blogs` 返回 `200`；
- `GET /api/blogs/0` 返回 `404`；
- 终止并重新运行后，接口仍能访问。

## 总结

最小 Controller API 的运行链路是：

```text
dotnet new
    ↓
创建 Controller
    ↓
AddControllers + MapControllers
    ↓
dotnet build
    ↓
dotnet run
    ↓
发送 HTTP Request
```

下一步可以分别学习项目文件、`Program.cs` Pipeline 和 Attribute Routing，而不需要把所有概念塞进创建项目这一个步骤。

## 参考资料

- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/)
- [.NET CLI](https://learn.microsoft.com/dotnet/core/tools/)

## 主线导航

- 下一步：[理解数据库组件职责](./01-aspnet-core-postgresql-ef-core-setup.md)
