---
title: "在本地启动并验证 ASP.NET Core API"
description: "理解 launchSettings、HTTP/HTTPS Profile、dotnet watch、.http 文件，以及如何区分启动、路由与 TLS 问题。"
tags:
  - ASP.NET Core
  - launchSettings
  - VS Code
  - HTTP
---

# 在本地启动并验证 ASP.NET Core API

API 调试应先证明 Server 已监听，再测试真实 Route，最后单独处理 HTTPS 或 Package Warning。把这些问题混在一起，会让一个简单的 404 看起来像应用完全无法启动。

## `launchSettings.json`

`Properties/launchSettings.json` 保存本地启动 Profile：

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:7000;http://localhost:5000"
    }
  }
}
```

它主要用于本地开发。Docker Container 通常不会读取这个文件。

## 使用指定 Profile 启动

```bash
dotnet run --launch-profile http
```

需要持续监控源码：

```bash
dotnet watch run --launch-profile http
```

`dotnet run` 不会监控文件；`dotnet watch` 才会在保存后尝试 Hot Reload 或重启应用。

## 先确认监听地址

```text
Now listening on: http://localhost:5000
Application started.
```

只有看到这些日志，才继续发送请求。不要只根据 IDE 是否显示绿色图标判断应用状态。

## 使用 `.http` 文件

```http
@host = http://localhost:5000

GET {{host}}/api/blogs
Accept: application/json

###

GET {{host}}/api/blogs/1
Accept: application/json
```

VS Code 可以通过 REST Client 等扩展发送请求。`###` 用于分隔多个请求。

## 根路径 404 不等于启动失败

如果 Controller 只定义 `/api/blogs`，访问：

```text
http://localhost:5000/
```

返回 `404` 是正常的。应测试 Controller 真正定义的 Route。

## HTTP Profile 的 HTTPS Warning

如果应用只监听 HTTP，但 Pipeline 中保留：

```csharp
app.UseHttpsRedirection();
```

可能出现无法确定 HTTPS Port 的 Warning。这个 Warning 与 Controller Route 404 是两个独立问题。

需要本地 HTTPS 时，可以信任 Development Certificate：

```bash
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

## OpenAPI 不等于 Swagger UI

```csharp
builder.Services.AddOpenApi();
app.MapOpenApi();
```

会生成 OpenAPI Document Endpoint，但不保证自动提供 Swagger UI 页面。是否存在可视化 UI 取决于项目额外安装和配置的工具。

## 排查顺序

```text
1. dotnet build 是否成功
2. 是否出现 Now listening on
3. 端口是否与请求一致
4. Controller Route 是否正确
5. HTTP Method 是否正确
6. 最后处理 HTTPS、OpenAPI 或 Package Warning
```

## 总结

启动、路由、TLS 和依赖安全是四个不同层面的问题。逐层验证，可以避免用重装 SDK 或修改 Docker 去解决一个单纯的 URL 错误。

## 参考资料

- [ASP.NET Core environments](https://learn.microsoft.com/aspnet/core/fundamentals/environments)
- [Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl)
- [.NET watch](https://learn.microsoft.com/dotnet/core/tools/dotnet-watch)
