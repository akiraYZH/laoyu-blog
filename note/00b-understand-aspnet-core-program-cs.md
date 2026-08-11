---
title: "读懂 ASP.NET Core 的 Program.cs"
description: "沿着 Builder、Service Registration、Middleware Pipeline 和 Endpoint Mapping，理解 Web API 从启动到处理请求的过程。"
tags:
  - ASP.NET Core
  - Program.cs
  - Middleware
  - Dependency Injection
---

# 读懂 ASP.NET Core 的 Program.cs

`Program.cs` 是 ASP.NET Core 应用的启动入口。它主要完成两件事：注册应用需要的 Service，以及定义 HTTP Request 经过的 Pipeline。

## 最小结构

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

## 创建 Builder

```csharp
var builder = WebApplication.CreateBuilder(args);
```

Builder 会准备默认 Configuration、Logging、Environment 和 Dependency Injection Container。

此时应用尚未启动，也没有开始监听端口。

## 注册 Service

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi();
```

注册阶段告诉容器应用以后需要哪些能力：

```text
AddControllers → Controller、Model Binding、Validation
AddOpenApi      → OpenAPI Document Generation
```

自定义 Service 和 EF Core `DbContext` 也在这个阶段注册。

## 构建应用

```csharp
var app = builder.Build();
```

这一步把已经完成的注册和配置构建成可以运行的 `WebApplication`。

Build 之后通常不再继续向 `builder.Services` 添加 Service。

## 配置 Pipeline

```csharp
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

Middleware 和 Endpoint Mapping 的顺序会影响请求处理：

```text
Request
  ↓
HTTPS Redirection
  ↓
Authorization
  ↓
Controller Endpoint
  ↓
Response
```

`Use...` 通常向 Pipeline 加入 Middleware；`Map...` 通常把可以处理请求的 Endpoint 加入路由系统。

## Development 条件

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

只有当前 Environment 是 Development 时，才暴露 OpenAPI Document Endpoint。Environment 与 Debug/Release Build Configuration 不是同一个概念。

## 启动 Server

```csharp
app.Run();
```

`Run()` 启动应用并持续监听 HTTP Request。它通常位于文件最后。

## `AddControllers` 与 `MapControllers`

两者不能互相替代：

```text
AddControllers → 注册 Controller 所需服务
MapControllers → 把 Attribute Route 变成 Endpoint
```

只有注册而没有 Mapping，Controller 不会匹配 URL；只有 Mapping 而缺少注册，Controller 所需能力不完整。

## 总结

```text
CreateBuilder → 准备配置和容器
Services      → 注册应用能力
Build         → 创建 WebApplication
Use / Map     → 定义 Request Pipeline
Run           → 启动并监听请求
```

## 参考资料

- [ASP.NET Core fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/)
- [ASP.NET Core Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
- [Dependency Injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)

