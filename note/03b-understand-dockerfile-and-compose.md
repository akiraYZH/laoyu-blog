---
title: "用职责模型读懂 Dockerfile 与 Docker Compose"
description: "不靠死记语法，使用构建单个镜像与编排多个服务的职责边界，读懂 ASP.NET Core、Vue 和 PostgreSQL 项目的容器配置。"
tags:
  - Docker
  - Dockerfile
  - Docker Compose
---

# 用职责模型读懂 Dockerfile 与 Docker Compose

Docker 配置不适合逐行背诵。真正需要记住的是两个文件分别回答什么问题：

```text
Dockerfile   → 一个 Image 怎么构建、Container 启动什么进程
compose.yaml → 多个 Service 怎么一起运行、连接和保存数据
```

本文不修改项目状态，而是建立一套可以重复使用的阅读方法。

## Dockerfile：描述一个 Image

阅读 Dockerfile 时依次回答六个问题：

| 指令 | 问题 |
|---|---|
| `FROM` | 从哪个基础 Image 开始？ |
| `WORKDIR` | 后续命令在哪个目录执行？ |
| `COPY` | 哪些文件进入 Image？ |
| `RUN` | 构建 Image 时执行什么？ |
| `EXPOSE` | 应用预期监听哪个端口？ |
| `CMD` / `ENTRYPOINT` | Container 启动时运行什么进程？ |

最小 ASP.NET Core 示例：

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BlogApi.dll"]
```

`RUN` 在 `docker build` 时执行，结果成为 Image Layer；`ENTRYPOINT` 在 `docker run` 时执行。多阶段构建让 SDK 留在 `build` 阶段，最终 `runtime` Image 只包含运行时和发布产物。

## Compose：描述运行关系

Compose 中每个 Service 都按七个问题检查：

```text
build/image → 容器从哪里来？
ports       → 主机怎样访问容器？
environment → 进程需要哪些配置？
volumes     → 哪些数据需要持久化或同步？
depends_on  → 启动依赖是什么？
healthcheck → 怎样判断服务真的可用？
command     → 是否覆盖 Image 的默认命令？
```

```yaml
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__LaoyuBlog: "Host=postgres;Port=5432;Database=blog;Username=blog_user;Password=${POSTGRES_PASSWORD}"
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:18-alpine
    environment:
      POSTGRES_DB: blog
      POSTGRES_USER: blog_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U blog_user -d blog"]

volumes:
  postgres_data:
```

Compose 会创建默认 Network，因此 API 使用 Service Name `postgres` 连接数据库，而不是 `localhost`。`ports` 的格式是 `主机端口:容器端口`；Container 之间通常直接使用容器端口。

## COPY 与 Volume 不相同

```text
COPY   → build 时把文件写入 Image
Volume → run 时把外部目录或 Named Volume 挂载给 Container
```

源代码或发布产物通常通过 `COPY` 进入 Image；PostgreSQL 数据必须使用 Volume，否则删除 Container 时数据也会消失。开发环境也可以用 Bind Mount 或 Compose Watch 同步源码，但生产 Image 不应依赖开发机目录。

## 验证而不是靠记忆

先让 Compose 展开环境变量并验证 YAML：

```bash
docker compose --env-file .env.development config --quiet
```

再分别验证构建与运行：

```bash
docker compose --env-file .env.development build
docker compose --env-file .env.development up
```

最后检查进程和日志：

```bash
docker compose ps
docker compose logs --tail=100 api
```

配置记不住不是问题。能从错误判断是 Build、Container Startup、Network、Environment 还是 Volume 问题，才是可迁移的能力。

## 总结

```text
Dockerfile：FROM → WORKDIR → COPY → RUN → EXPOSE → ENTRYPOINT
Compose：services → build/image → ports → environment → volumes → depends_on → healthcheck
```

先记职责，再查语法；先写最小配置，再用 `docker compose config`、`build` 和 `up` 验证。

## 参考资料

- [Dockerfile reference](https://docs.docker.com/reference/dockerfile/)
- [Compose file reference](https://docs.docker.com/reference/compose-file/)
- [Containerize a .NET application](https://docs.docker.com/guides/dotnet/containerize/)

## 关联主线

- [使用多阶段 Dockerfile 构建 ASP.NET Core API Image](./03-containerize-aspnet-core-api-with-docker.md)
- [把 ASP.NET Core API 与 PostgreSQL 放入 Docker Compose](./03a-add-aspnet-core-api-to-compose.md)
