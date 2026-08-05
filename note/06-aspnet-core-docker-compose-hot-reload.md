---
title: "使用 Docker Compose Watch 为 ASP.NET Core API 实现热更新"
description: "区分 Compose Watch 与 dotnet watch 的职责，并为 .NET 10 API 配置开发镜像、源码同步和生产运行阶段。"
tags:
  - .NET 10
  - ASP.NET Core
  - Docker Compose
  - Hot Reload
---

# 使用 Docker Compose Watch 为 ASP.NET Core API 实现热更新

普通 Docker 开发流程通常是：修改 C# 文件、重新构建 Image、重新创建 Container。功能没有问题，但每次改动都执行完整构建，会打断 API 开发节奏。

热更新要解决的是：保存 C# 文件后，让正在运行的开发 Container 使用新代码，同时继续保留精简的生产 Image。

## 热更新需要两个组件

Compose Watch 和 `dotnet watch` 解决的是两个不同问题：

```text
开发机上的 C# 文件
        │
        │ Compose Watch：同步文件
        ▼
Container 中的 /src
        │
        │ dotnet watch：检测改动
        ▼
Hot Reload 或重启 API
```

- Compose Watch 负责把开发机上的文件变化同步到 Container。
- `dotnet watch` 负责监控 Container 内的项目，并应用 Hot Reload。

只有 Compose Watch，没有 `dotnet watch`，文件虽然进入了 Container，运行中的 .NET 进程却不会自动使用新代码。

只有 `dotnet watch`，没有源码挂载或同步，Container 内也看不到开发机上的变化。

## 为 Dockerfile 增加开发阶段

开发环境需要 .NET SDK 才能运行 `dotnet watch`；生产环境只需要 ASP.NET Runtime。因此，可以在多阶段 Dockerfile 中加入独立的 `development` 阶段：

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development

WORKDIR /src

COPY ["BlogApi.csproj", "./"]
RUN dotnet restore "BlogApi.csproj"

COPY . .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

EXPOSE 8080

CMD ["dotnet", "watch", "run", "--no-launch-profile"]


FROM development AS build

RUN dotnet publish "BlogApi.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "BlogApi.dll"]
```

关键点只有三个。

### `development` 使用 SDK Image

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
```

SDK Image 包含编译器、MSBuild 和 `dotnet watch`，适合开发，但不应该作为最终生产 Image。

### 使用轮询监控 Container 文件

```dockerfile
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
```

在 Docker Desktop 和文件同步场景中，文件系统事件不一定能够稳定穿过宿主机与 Container 边界。启用轮询后，`dotnet watch` 会主动检查文件变化。

### 开发 Container 启动 `dotnet watch`

```dockerfile
CMD ["dotnet", "watch", "run", "--no-launch-profile"]
```

`--no-launch-profile` 避免读取开发机专用的 `launchSettings.json`。Container 的监听端口由 `ASPNETCORE_HTTP_PORTS` 控制。

最终的 `runtime` 阶段仍然只复制发布结果，不包含 SDK 和源码。

## 配置 Compose Watch

在开发环境的 `compose.yaml` 中，让 API 构建 `development` 阶段，并加入文件同步规则：

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    develop:
      watch:
        - action: sync
          path: .
          target: /src
          ignore:
            - bin/
            - obj/
            - .git/
            - .env*
            - note/
            - docs/
            - Dockerfile
            - compose.yaml
            - Makefile
            - "*.csproj"
            - "*.http"
```

配置的含义是：

- `target: development`：选择 Dockerfile 中包含 SDK 的开发阶段。
- `action: sync`：同步变化，而不是每次重新构建整个 Image。
- `path: .`：监控项目目录。
- `target: /src`：把变化写入 Container 的项目目录。
- `ignore`：排除构建输出、密钥文件和不影响 API 运行的文档。

这里忽略了 `*.csproj`。修改 NuGet 依赖后应重新构建 Image，而不是只同步项目文件：

```bash
docker compose up --detach --build api
```

## 用 Makefile 区分普通启动与开发模式

不要让 `up` 同时承担后台启动和前台监控两个含义。可以保留普通启动，再增加 `dev`：

```makefile
ENV_FILE ?= .env.development
COMPOSE = docker compose --env-file $(ENV_FILE)

.PHONY: up dev down

up:
	$(COMPOSE) up --detach --build

dev:
	$(COMPOSE) watch

down:
	$(COMPOSE) down
```

普通后台启动：

```bash
make up
```

开发热更新：

```bash
make dev
```

`make dev` 会持续占用当前终端，因为 Compose 正在监控文件。停止 Watch 可以按 `Ctrl+C`。

## 验证配置

先检查 Compose YAML 与环境变量插值：

```bash
docker compose \
  --env-file .env.development \
  config --quiet
```

命令没有输出且退出码为 `0`，表示配置有效。

然后启动开发模式：

```bash
make dev
```

日志中应出现类似内容：

```text
dotnet watch
Now listening on: http://[::]:8080
```

接着修改一个 Controller 的方法体并保存。正常情况下会看到文件同步，以及 `dotnet watch` 应用 Hot Reload 或重启应用的日志。

最后重新发送 HTTP 请求，确认响应已经使用新代码：

```bash
curl -i http://localhost:8080/api/blogs
```

## 常见误区

### 保存文件不会自动执行 Compose

只有 `docker compose watch` 或 `docker compose up --watch` 正在运行时，Compose 才会监控文件。关闭 Watch 后，保存文件不会自动启动任何 Container。

### `sync` 不是重新构建 Image

`action: sync` 只更新运行中 Container 的文件。它适合 C# 源码变化；Dockerfile、基础 Image或 NuGet 依赖变化仍然需要重新构建。

### Hot Reload 不等于永远不重启

部分方法体修改可以直接应用 Hot Reload。某些结构性修改无法热替换时，`dotnet watch` 会重新构建或重启应用，这仍然属于正常的开发监控流程。

### 开发阶段不能替代生产阶段

开发阶段包含 SDK、源码和 Watch 工具，体积较大。生产部署仍应使用最后的 `runtime` 阶段，只包含发布结果和 ASP.NET Runtime。

## 总结

ASP.NET Core Container 热更新不是一条命令单独完成的：

```text
Compose Watch 解决文件如何进入 Container
dotnet watch 解决运行中的 API 如何使用新代码
development stage 提供编译和监控工具
runtime stage 保持生产 Image 精简
```

把开发和生产阶段分开后，可以获得更快的本地反馈，同时不牺牲生产 Image 的安全性和体积。

## 参考资料

- [Docker Compose Watch](https://docs.docker.com/compose/how-tos/file-watch/)
- [dotnet watch](https://learn.microsoft.com/dotnet/core/tools/dotnet-watch)
