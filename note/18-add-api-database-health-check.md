---
title: "为 ASP.NET Core API 和 PostgreSQL 添加 Health Check"
description: "使用 AddDbContextCheck 暴露数据库健康端点，并让 Docker Compose 等待 API 真正可用后再启动前端。"
tags:
  - ASP.NET Core
  - Health Check
  - EF Core
  - Docker Compose
---

# 为 ASP.NET Core API 和 PostgreSQL 添加 Health Check

Container 进程正在运行，不代表 API 已经能够访问数据库。若前端只依赖 API Container 启动，可能在 API 初始化或 PostgreSQL 尚不可用时立即发送请求。

本文建立一条可验证的健康链：

```text
PostgreSQL healthy
    ↓
API /health 能连接 AppDbContext
    ↓
API Container healthy
    ↓
Frontend 启动
```

## 安装 EF Core Health Check

基础 Health Check 由 ASP.NET Core 提供；检查 `DbContext` 需要额外 Package：

```bash
dotnet add package \
  Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore \
  --version 10.0.10
```

Package 版本应与项目使用的 EF Core 主版本保持一致。

## 注册数据库检查

在 `Program.cs` 的 `AddDbContext` 后注册：

```csharp
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();
```

默认检查会调用 EF Core 的 `CanConnectAsync()`。它确认应用能否使用当前 `AppDbContext` 配置连接数据库，但不会验证所有表结构或业务查询。

映射公开端点：

```csharp
app.MapHealthChecks("/health");
app.MapControllers();
```

健康时返回 `200 OK` 和 `Healthy`；数据库检查失败时默认返回 `503 Service Unavailable`。

## 让 Docker 检查 API

Healthcheck 命令在 Container 内执行，因此 Image 必须包含探测工具。可在 Dockerfile 对相应阶段安装 `curl`：

```dockerfile
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
```

在 `compose.yaml` 的 API Service 添加：

```yaml
healthcheck:
  test:
    ["CMD", "curl", "--fail", "--silent", "http://localhost:8080/health"]
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 15s
```

这里的 `localhost` 指 API Container 自己，不是开发机，也不是 PostgreSQL Container。

前端使用长格式依赖：

```yaml
depends_on:
  api:
    condition: service_healthy
```

普通 `depends_on: [api]` 只控制启动顺序；`service_healthy` 才等待 Healthcheck 成功。

## 添加集成测试

```csharp
[Fact]
public async Task Health_WhenApiAndDatabaseAreHealthy_ReturnsOk()
{
    var response = await _client.GetAsync("/health");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

这证明测试 Host 的 API 与测试 DbContext 能通过检查。它没有模拟 PostgreSQL 断线，因此不能代替实际故障演练。

## 验证

```bash
dotnet build
dotnet test
docker compose --env-file .env.development config --quiet
docker compose --env-file .env.development build api
docker compose --env-file .env.development up -d api
```

查看状态：

```bash
docker compose ps
curl --fail http://localhost:8080/health
```

预期 API 显示 `healthy`，请求输出 `Healthy`。

## 常见错误

### Container 一直 unhealthy

先在 API Container 中确认 `curl` 存在，并检查应用是否监听 8080。开发和生产使用不同 Docker Stage 时，两个实际运行的 Stage 都必须具备探测工具。

### PostgreSQL 正常但 API 返回 503

`pg_isready` 只证明 PostgreSQL 接受连接；API 仍可能使用错误的 Host、Port、Database、Username 或 Password。检查 ASP.NET Core 实际读取的 Connection String。

## 总结

`/health` 现在同时代表 API 能响应和 `AppDbContext` 能连接数据库；Compose 不再把“进程启动”误认为“服务可用”。

## 参考资料

- [Health checks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [Control startup order in Compose](https://docs.docker.com/compose/how-tos/startup-order/)

## 主线导航

- 上一步：[使用 WebApplicationFactory 测试授权与发布流程](./17-test-blog-api-workflows.md)
- 下一步：[使用日志 Scope 关联用户、请求与文章操作](./19-structured-logging-request-scope.md)
