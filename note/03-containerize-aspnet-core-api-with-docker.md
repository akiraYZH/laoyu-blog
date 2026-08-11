---
title: "使用多阶段 Dockerfile 构建 ASP.NET Core API Image"
description: "理解 SDK 与 Runtime Stage、Build Cache、dotnet publish、非 root 用户，以及为什么源码不会进入最终 Image。"
tags:
  - ASP.NET Core
  - Docker
  - Dockerfile
  - Multi-stage Build
---

# 使用多阶段 Dockerfile 构建 ASP.NET Core API Image

本文只解决一个问题：为 ASP.NET Core API 构建一个不包含 SDK 和源码的 Runtime Image。

## 完整 Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BlogApi.csproj", "./"]
RUN dotnet restore "BlogApi.csproj"

COPY . .
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

## Build Stage 使用 SDK

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

SDK Image 包含 Restore、Build 和 Publish 工具。`AS build` 为这一 Stage 命名，方便后面复制发布结果。

```dockerfile
WORKDIR /src
```

后续相对路径和命令都以 `/src` 为当前目录。

## 为什么先复制 csproj

```dockerfile
COPY ["BlogApi.csproj", "./"]
RUN dotnet restore "BlogApi.csproj"
```

NuGet Dependency 通常比业务源码变化少。先单独复制项目文件，可以让 Docker 重用 Restore Layer Cache。

```text
csproj 未改变 → 复用 restore cache
csproj 改变   → 重新 restore
```

Cache 来自 Docker Build Layer，而不是因为命令恰好使用了 `RUN`。

## 复制源码并发布

```dockerfile
COPY . .
```

源码必须进入 Build Stage，因为 `dotnet publish` 需要编译源码。

```dockerfile
RUN dotnet publish "BlogApi.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore
```

参数含义：

```text
Release      → 使用发布构建配置
/app/publish → 集中保存运行所需文件
--no-restore → 复用前一步 Restore 结果
```

## Runtime Stage 重新开始

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
```

第二个 `FROM` 开始一条新的 Image Layer Chain。ASP.NET Runtime Image 可以运行 API，但不包含完整 SDK。

```dockerfile
COPY --from=build /app/publish .
```

这是两个 Stage 之间唯一明确的数据传递，只复制 `/app/publish`，不会复制 `/src`。

因此最终 Image 包含：

```text
Runtime
DLL
Dependency
Runtime Configuration
```

不会包含：

```text
.cs Source
.NET SDK
NuGet Restore Cache
Build Intermediate Files
```

## 为什么不能复制后再删除源码

Docker Image 由 Layer 组成。在同一 Stage 中先复制源码、再用后续 Layer 删除，旧 Layer 仍可能包含源码。

Multi-stage Build 从独立 Runtime Stage 开始，并且从未把源码复制进去，因此最终 Layer Chain 更干净。

## 端口与启动命令

```dockerfile
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
```

环境变量让 ASP.NET Core 监听 8080；`EXPOSE` 记录 Image 预期端口，但不会自动发布到开发机。

```dockerfile
ENTRYPOINT ["dotnet", "BlogApi.dll"]
```

Container 启动时运行已发布的 DLL。

## 使用非 root 用户

```dockerfile
USER $APP_UID
```

官方 ASP.NET Runtime Image 提供非 root `app` 用户，并通过 `APP_UID` 暴露它的 UID。API 通常不需要 root 权限，减少权限可以降低 Container 被利用后的影响范围。

## `.dockerignore`

```dockerignore
bin/
obj/
.git/
.env
.env.*
note/
```

`.dockerignore` 控制哪些文件进入 Docker Build Context。`.gitignore` 不能代替它。

环境文件和 Secret 不应该进入 Context、Build Log 或 Intermediate Layer。

## 构建和运行

```bash
docker build --tag blog-api:local .
```

```bash
docker run --rm \
  --publish 8080:8080 \
  blog-api:local
```

验证：

```bash
curl -i http://localhost:8080/api/blogs
```

## 检查最终 Image

```bash
docker run --rm \
  --entrypoint sh \
  blog-api:local \
  -c 'find /app -name "*.cs" -o -name "*.csproj"'
```

没有输出表示 Runtime Stage 中没有匹配的源码文件。

## 总结

```text
Build Stage   → SDK + Source → publish
Runtime Stage → Runtime + publish output
```

源码进入 Build Stage 是编译需要；源码不进入最终 Image，是因为第二个 `FROM` 开始独立 Stage，并且只复制 Publish Output。

## 参考资料

- [.NET Docker images](https://learn.microsoft.com/dotnet/core/docker/container-images)
- [Docker multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [Docker build cache](https://docs.docker.com/build/cache/)

## 主线导航

- 上一步：[加入 Request Validation](./08-aspnet-core-dto-common-attributes.md)
- 下一步：[把 API 加入 Compose](./03a-add-aspnet-core-api-to-compose.md)
