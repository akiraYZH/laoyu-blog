---
title: "把 ASP.NET Core API 与 PostgreSQL 放入 Docker Compose"
description: "配置 API Service、PostgreSQL Service DNS、Connection String、Health Dependency 和端口映射。"
tags:
  - ASP.NET Core
  - Docker Compose
  - PostgreSQL
  - Npgsql
---

# 把 ASP.NET Core API 与 PostgreSQL 放入 Docker Compose

API Image 和 PostgreSQL Container 分别可用后，下一步是让 Compose 统一启动它们，并让 API 通过 Compose Network 连接数据库。

## Compose 配置

```yaml
services:
  postgres:
    image: postgres:18-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql
    healthcheck:
      test:
        - CMD-SHELL
        - pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}
      interval: 5s
      timeout: 5s
      retries: 10

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: >-
        Host=postgres;Port=5432;Database=${POSTGRES_DB};
        Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:
```

## API Build 配置

```yaml
build:
  context: .
  dockerfile: Dockerfile
```

`context` 决定 Docker Build 可以读取哪些文件；`dockerfile` 指定构建说明文件。

Build Context 应由 `.dockerignore` 排除 Secret、生成目录和无关文件。

## API 端口映射

```yaml
ports:
  - "8080:8080"
```

开发机通过 `localhost:8080` 访问 API，Container 内的 ASP.NET Core 监听 8080。

端口映射只解决开发机到 Container 的访问，不决定 API 如何连接 PostgreSQL。

## Container 之间使用 Service Name

Connection String 使用：

```text
Host=postgres
```

原因是 Compose 会为 Service 提供内部 DNS：

```text
api Container → postgres:5432
```

API Container 中的 `localhost` 指向 API Container 自己，不是 PostgreSQL Container。

开发机上的 Migration CLI 则通常使用：

```text
Host=localhost
```

两者运行在不同网络环境中。

## Environment Variable 映射

```yaml
ConnectionStrings__DefaultConnection: ...
```

ASP.NET Core Configuration 会把双下划线转换为层级分隔符：

```text
ConnectionStrings__DefaultConnection
                ↓
ConnectionStrings:DefaultConnection
```

C# 可以读取：

```csharp
builder.Configuration
    .GetConnectionString("DefaultConnection");
```

## 等待数据库健康

```yaml
depends_on:
  postgres:
    condition: service_healthy
```

Compose 会等待 PostgreSQL Healthcheck 通过后再启动 API。

它只能解决启动顺序，不能保证数据库 Schema 已经应用，也不能代替应用自身的重试、Health Endpoint 或 Migration Deployment Strategy。

## 验证配置和运行

```bash
docker compose \
  --env-file .env.development \
  config --quiet
```

```bash
docker compose \
  --env-file .env.development \
  up --detach --build
```

查看状态：

```bash
docker compose ps
```

查看 API 日志：

```bash
docker compose logs api --tail 50
```

预期出现：

```text
Now listening on: http://[::]:8080
Application started.
```

最后验证真实 Route：

```bash
curl -i http://localhost:8080/api/blogs
```

## Development 与 Production

Compose 文件可以复用 Service 结构，但 Production 不应直接复用本地密码、Development Environment 或公开数据库端口。

Production 通常需要：

- Secret Manager 或平台 Secret；
- HTTPS 和 Reverse Proxy；
- 独立 Migration Step；
- 不向公网发布 PostgreSQL Port；
- Production Environment；
- Image Registry 中的固定版本。

## 总结

```text
开发机 → localhost:8080 → API Container
API    → postgres:5432  → PostgreSQL Container
```

Compose 统一管理运行关系；Service Name 解决 Container DNS；Connection String Environment Variable 把数据库配置传入 ASP.NET Core。

## 参考资料

- [Docker Compose networking](https://docs.docker.com/compose/how-tos/networking/)
- [ASP.NET Core configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Compose startup order](https://docs.docker.com/compose/how-tos/startup-order/)

## 主线导航

- 上一步：[构建 API Docker Image](./03-containerize-aspnet-core-api-with-docker.md)
- 下一步：[配置 Container 热更新](./06-aspnet-core-docker-compose-hot-reload.md)
