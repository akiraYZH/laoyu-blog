---
title: "使用多阶段 Dockerfile 容器化 ASP.NET Core API"
description: "逐行理解 .NET 10 API 的 Dockerfile：SDK 与 Runtime Image、构建缓存、dotnet publish、非 root 用户，以及为什么源码不会进入最终 Image。"
tags:
  - .NET 10
  - ASP.NET Core
  - Docker
  - Dockerfile
  - Multi-stage Build
---

# 使用多阶段 Dockerfile 容器化 ASP.NET Core API

把 PostgreSQL 放进 Docker Compose 后，下一步可以为 ASP.NET Core API 创建 Docker Image。这样开发环境不再依赖开发机上恰好安装了哪些运行时，也为后续把 API 和数据库一起交给 Compose 管理做好准备。

不过，Dockerfile 不只是把几条命令写在一起。要真正理解它，需要先回答几个问题：

- 为什么构建阶段使用 .NET SDK Image，运行阶段却使用 ASP.NET Runtime Image？
- 为什么先复制项目文件，再复制全部源码？
- Docker 的构建缓存是不是由 `RUN` 实现的？
- 为什么源码必须复制进构建阶段？
- 既然复制了源码，为什么最终 Image 中却没有源码？

本文使用一个名为 `BlogApi` 的通用 ASP.NET Core API 作为示例，逐行解释这些问题。

## 最终 Dockerfile

在项目根目录创建 `Dockerfile`：

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

这是一个多阶段 Dockerfile，包含两个互相独立的阶段：

```text
Build Stage
SDK + 项目文件 + 源码
        ↓
dotnet restore
        ↓
dotnet publish
        ↓
/app/publish
        │
        │ 只复制发布结果
        ▼
Runtime Stage
ASP.NET Runtime + 发布结果
        ↓
启动 BlogApi.dll
```

## 第一阶段：构建并发布 API

### 选择 .NET SDK Image

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

`FROM` 指定这一阶段的基础 Image。

这里使用 `sdk:10.0`，因为构建项目需要完整的 .NET SDK。它包含：

- `dotnet restore`；
- C# 编译器；
- MSBuild；
- `dotnet build`；
- `dotnet publish`。

`AS build` 给当前阶段起名为 `build`。后面的运行阶段会使用这个名称，从构建阶段取出发布结果。

这一步使用 SDK，不代表生产环境也需要 SDK。SDK 只负责把源码转换成可运行的发布文件。

### 设置构建工作目录

```dockerfile
WORKDIR /src
```

`WORKDIR` 把 Container 内的当前目录设置为 `/src`。后续相对路径都以这里为起点。

例如：

```dockerfile
COPY ["BlogApi.csproj", "./"]
```

会把项目文件复制为：

```text
/src/BlogApi.csproj
```

如果 `/src` 不存在，Docker 会自动创建它。

### 为什么先只复制项目文件

```dockerfile
COPY ["BlogApi.csproj", "./"]
```

这一步暂时不复制全部源码，只复制描述项目和 NuGet 依赖的 `.csproj` 文件。

这样安排主要是为了提高 Docker 构建缓存的命中率。NuGet 依赖通常不会像业务源码一样频繁变化，因此可以先建立一个只依赖 `.csproj` 的 Restore Layer。

### 恢复 NuGet 依赖

```dockerfile
RUN dotnet restore "BlogApi.csproj"
```

`RUN` 会在当前构建阶段中执行命令。`dotnet restore` 读取项目文件，并恢复项目声明的 NuGet Package。

例如，项目中可能包含：

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
```

Restore 会根据这些声明准备后续编译所需的依赖。

当源码发生变化、但 `.csproj` 没有变化时，Docker 通常可以复用这一步的缓存，不必重新下载全部 NuGet Package。

### 复制源码

```dockerfile
COPY . .
```

第一个 `.` 表示 Docker Build Context 中的当前目录，第二个 `.` 表示 Container 当前工作目录 `/src`。

因此，这行可以理解为：

```text
将 Build Context 中允许复制的文件复制到 /src
```

这里必须复制源码，因为 `dotnet publish` 需要读取：

- `.cs` 源文件；
- `.csproj` 项目配置；
- `appsettings.json`；
- 项目引用；
- 需要随应用发布的静态文件。

Image 的构建发生在隔离的构建环境中。它不能凭空访问开发机上的项目目录，因此必须通过 `COPY` 把构建所需输入送进去。

### 发布 Release 版本

```dockerfile
RUN dotnet publish "BlogApi.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore
```

这条命令把项目编译并整理成可以部署的发布结果。

各参数的含义如下：

| 参数 | 含义 |
|---|---|
| `BlogApi.csproj` | 指定要发布的项目 |
| `--configuration Release` | 使用 Release 配置构建 |
| `--output /app/publish` | 把发布结果集中写入 `/app/publish` |
| `--no-restore` | 不重复 Restore，使用前面已经恢复的依赖 |

发布目录通常包含：

```text
/app/publish
├── BlogApi.dll
├── BlogApi.deps.json
├── BlogApi.runtimeconfig.json
├── appsettings.json
└── 依赖程序集
```

应用运行需要的是这些发布文件，而不是完整 SDK、NuGet 下载缓存或原始 C# 源码。

## Docker 缓存是不是通过 RUN 实现的

不是。

缓存是 Docker Builder 提供的构建机制，不是 `RUN` 命令自己实现的。Dockerfile 中的多种指令都可能形成可复用的构建结果，例如：

```dockerfile
FROM ...
WORKDIR ...
COPY ...
RUN ...
```

当 Docker 重新构建 Image 时，会检查某一步的指令和输入是否仍然相同。如果相同，就可能复用之前的结果；如果不同，这一步以及后面的相关步骤通常需要重新执行。

当前顺序的价值在于：

```dockerfile
COPY ["BlogApi.csproj", "./"]
RUN dotnet restore "BlogApi.csproj"
COPY . .
RUN dotnet publish ...
```

假设只修改了 `Controllers/PostsController.cs`：

```text
COPY .csproj       → 可以复用缓存
RUN restore        → 可以复用缓存
COPY 全部源码      → 输入变化，需要重新执行
RUN publish        → 需要重新执行
```

如果一开始就执行：

```dockerfile
COPY . .
RUN dotnet restore
```

那么修改任何进入 Build Context 的源文件，都可能使 `COPY . .` 的结果变化，从而让后面的 Restore 缓存失效。

所以，`RUN` 会产生可缓存的构建结果，但缓存能力来自 Docker；把较稳定的文件放在前面复制，是在主动利用这套缓存机制。

## 为什么不直接复制开发机 Build 后的结果

也可以先在开发机执行：

```bash
dotnet publish --configuration Release --output ./publish
```

然后使用一个只复制 `publish` 目录的 Dockerfile：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "BlogApi.dll"]
```

但这种方式会把“如何正确构建应用”的责任留给开发机或 CI 环境：

- 构建机必须安装正确的 .NET SDK；
- 构建步骤需要在 Docker 之外额外执行；
- 不同开发机可能使用不同 SDK 或构建参数；
- 单独执行 `docker build` 不再足以生成完整应用 Image。

多阶段构建则把 SDK 版本和发布命令一起写进 Dockerfile：

```text
源码 + Dockerfile
        ↓
docker build
        ↓
可运行 Image
```

这样更容易在开发机、CI 和服务器上重复得到一致结果。

## 第二阶段：创建最终运行 Image

### 从 ASP.NET Runtime 重新开始

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
```

第二个 `FROM` 会开始一个新的构建阶段。它不是继续修改 `build` 阶段，而是以 `aspnet:10.0` 为新的基础 Image。

`aspnet:10.0` 包含运行 ASP.NET Core 应用所需的 Runtime，但不包含完整 SDK 和编译工具。它适合最终运行环境，因为最终 Container 只需要运行已经发布的应用。

SDK Image 与 ASP.NET Runtime Image 的职责可以概括为：

| Image | 主要用途 | 是否包含编译工具 |
|---|---|---|
| `mcr.microsoft.com/dotnet/sdk:10.0` | Restore、Build、Publish | 是 |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | 运行 ASP.NET Core 应用 | 否 |

### 设置运行目录

```dockerfile
WORKDIR /app
```

最终 Container 启动后以 `/app` 为工作目录。接下来复制的发布文件和应用入口都会位于这里。

### 只复制发布结果

```dockerfile
COPY --from=build /app/publish .
```

这行是两个阶段之间唯一的数据传递：

- `--from=build`：从名为 `build` 的阶段读取文件；
- `/app/publish`：选择构建阶段中的发布目录；
- `.`：复制到当前阶段的工作目录 `/app`。

它不会复制整个 `build` 阶段，只会复制明确指定的 `/app/publish`。

### 配置应用监听端口

```dockerfile
ENV ASPNETCORE_HTTP_PORTS=8080
```

这项环境变量告诉 ASP.NET Core 在 Container 内监听 HTTP 8080 端口。

```dockerfile
EXPOSE 8080
```

`EXPOSE` 用来声明这个 Image 预期通过 8080 提供服务，但它不会自动把端口发布到开发机。

真正运行 Container 时仍然需要端口映射，例如：

```bash
docker run --rm -p 8080:8080 blog-api
```

两个 `8080` 分别代表：

```text
开发机端口 : Container 端口
```

### 使用非 root 用户

```dockerfile
USER $APP_UID
```

.NET 官方 Linux Container Image 提供了用于非 root 运行的应用用户。`APP_UID` 表示该用户的 UID。

这行让后续的应用进程不以 root 身份运行，从而减少应用被利用后能够获得的 Container 权限。

非 root 用户通常不能绑定 Linux 的特权端口，因此这里使用 8080，而不是 80。

### 定义启动命令

```dockerfile
ENTRYPOINT ["dotnet", "BlogApi.dll"]
```

当 Container 启动时，Docker 会执行：

```bash
dotnet BlogApi.dll
```

这里使用 JSON 数组形式，可以让应用进程更直接地接收停止信号，也避免额外经过 Shell 解析。

## 为什么源码不会进入最终 Image

虽然第一阶段执行了：

```dockerfile
COPY . .
```

但它只把源码复制到了 `build` 阶段的 `/src`。

两个阶段的文件系统关系如下：

```text
第一阶段：build                     第二阶段：runtime
----------------                    ------------------
基础：dotnet/sdk:10.0               基础：dotnet/aspnet:10.0
/src/BlogApi.csproj                 /app/BlogApi.dll
/src/Program.cs                     /app/*.json
/src/Controllers/...                /app/依赖程序集
/app/publish/...                    没有 /src
```

原因是第二个 `FROM` 创建了新的阶段和新的 Image Layer 链：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
```

它不会自动继承 `build` 阶段的文件系统。两个阶段之间只有这条明确的复制操作：

```dockerfile
COPY --from=build /app/publish .
```

因此：

```text
/app/publish  → 被复制到最终 Image
/src          → 没有被复制，不属于最终 Image
.NET SDK      → 来自第一阶段，不属于最终 Image
构建缓存      → 不属于最终运行 Image
```

默认情况下，`docker build` 生成的是 Dockerfile 的最后一个阶段。前面的阶段可以保留在开发机的 Build Cache 中，帮助后续加快构建，但它们不是最终 Image 的文件系统内容，也不会因为构建最终 Image 就自动成为运行 Container 的一部分。

## 为什么不能在单阶段中复制源码后再删除

一种看似可行的写法是：

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0
COPY . /src
RUN dotnet publish ...
RUN rm -rf /src
```

问题在于 Docker Image 由多层组成。后面的删除操作只是在新 Layer 中标记文件已被删除，并不会重写已经包含源码的早期 Layer。能够取得完整 Image Layer 的人仍可能检查早期 Layer。

多阶段构建没有在最终阶段复制源码，因此最终 Image 的 Layer 链从一开始就不包含 `/src`。这比“先复制、再删除”更加干净。

需要注意：多阶段构建不能代替 Secret 管理。如果密码、私钥或本地环境文件进入了 Build Context 或参与构建，它们仍可能出现在构建缓存、日志或其他中间产物中。因此还需要正确配置 `.dockerignore`，并避免在 Dockerfile 中复制或输出 Secret。

## 配置 .dockerignore

项目根目录还应创建 `.dockerignore`：

```dockerignore
# .NET build output
bin/
obj/

# Version control and editor files
.git/
.vscode/
.DS_Store

# Local secrets
.env
.env.*

# Files not needed to build the API
note/
docs/
*.http
```

`.dockerignore` 的作用类似 `.gitignore`，但它控制的是哪些文件不会进入 Docker Build Context。

它带来三个直接好处：

- 减少发送给 Docker Builder 的文件数量；
- 避免无关文件变化导致 `COPY . .` 缓存失效；
- 降低本地 Secret 被意外复制进构建阶段的风险。

`.gitignore` 和 `.dockerignore` 不能互相代替。一个文件即使没有提交到 Git，只要没有被 `.dockerignore` 排除，仍可能进入 Docker Build Context。

## 构建和运行 Image

在包含 Dockerfile 的项目根目录执行：

```bash
docker build --tag blog-api:development .
```

参数含义如下：

| 参数 | 含义 |
|---|---|
| `docker build` | 根据 Dockerfile 构建 Image |
| `--tag blog-api:development` | 设置 Image 名称和 Tag |
| 最后的 `.` | 使用当前目录作为 Build Context |

构建完成后运行：

```bash
docker run --rm --publish 8080:8080 blog-api:development
```

然后访问实际 Controller 对应的路由。例如 Controller 使用：

```csharp
[Route("api/[controller]")]
public class PostsController : ControllerBase
```

对应地址可能是：

```text
http://localhost:8080/api/posts
```

不要只访问根路径 `/` 来判断 API 是否成功。Controller API 没有定义根路由时，访问 `/` 返回 404 是正常行为。

## 验证最终 Image 中没有源码

可以临时覆盖 `ENTRYPOINT`，进入最终 Image 检查文件：

```bash
docker run --rm \
  --entrypoint /bin/sh \
  blog-api:development \
  -c 'find /app -maxdepth 2 -type f | sort'
```

预期可以看到 DLL、JSON 配置和依赖程序集，但不应该看到：

```text
/src/Program.cs
/src/Controllers/
/src/BlogApi.csproj
```

如果 Dockerfile 改为：

```dockerfile
FROM build AS runtime
```

那么运行阶段就会基于 `build` 阶段继续构建，`/src` 也会被继承。这正好反向证明：源码是否进入最终 Image，取决于最后阶段使用什么基础，以及明确复制了哪些文件。

## 把 API 加入 Docker Compose

Dockerfile 解决的是“如何构建并运行一个 API Container”，Compose 解决的则是“如何统一管理 API 和 PostgreSQL 两个 Service”。

加入 API 后，本地开发环境的结构变为：

```text
Browser
   │
   │ http://localhost:8080
   ▼
ASP.NET Core API Container
   │
   │ Compose 内部网络
   ▼
PostgreSQL Container
```

在 `compose.yaml` 的 `services` 下增加 `api`：

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:18-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql
    healthcheck:
      test:
        [
          "CMD-SHELL",
          "pg_isready -U $${POSTGRES_USER} -d $${POSTGRES_DB}"
        ]
      interval: 5s
      timeout: 5s
      retries: 5
      start_period: 10s

volumes:
  postgres_data:
```

这里没有给 API 单独设置 `image`。Compose 会根据 `build` 配置构建本地 Image，并自动使用它创建 API Container。

## 逐行理解 API Service

### 定义 API Service

```yaml
api:
```

`api` 是 Compose Service 名称。Compose 命令可以用它单独指定 API：

```bash
docker compose build api
docker compose logs api
docker compose restart api
```

Service 名称也会成为 Compose 默认网络中的 DNS 名称。其他 Container 可以通过 `api` 找到这个 Service，但开发机上的浏览器仍然要使用 `localhost` 和已发布端口。

### 告诉 Compose 如何构建 Image

```yaml
build:
  context: .
  dockerfile: Dockerfile
```

`build` 表示 API 不直接使用远程仓库中的现成 Image，而是根据本地项目进行构建。

`context: .` 指定当前项目目录为 Docker Build Context。Dockerfile 中的：

```dockerfile
COPY . .
```

复制的就是这个 Context 中没有被 `.dockerignore` 排除的文件。

`dockerfile: Dockerfile` 指定构建配置文件。虽然文件使用默认名称时可以省略这一行，但明确写出后，阅读 Compose 文件时更容易理解构建来源；未来改用 `Dockerfile.development` 等名称时，也可以直接在这里调整。

### 发布 API 端口

```yaml
ports:
  - "8080:8080"
```

端口映射格式是：

```text
开发机端口 : Container 端口
```

左边的 `8080` 让开发机可以通过以下地址访问 API：

```text
http://localhost:8080
```

右边的 `8080` 对应 Dockerfile 中的：

```dockerfile
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
```

Dockerfile 中的 `EXPOSE` 只是描述 Image 预期使用哪个端口，Compose 的 `ports` 才真正把 Container 端口发布到开发机。

如果只希望开发机本机访问、而不绑定所有网络接口，可以进一步写成：

```yaml
ports:
  - "127.0.0.1:8080:8080"
```

### 设置 ASP.NET Core 环境

```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Development
```

这会把环境变量传入 API Container。ASP.NET Core 因而使用 Development 环境，并读取：

```text
appsettings.json
        +
appsettings.Development.json
        +
Container 环境变量
```

需要区分两种环境变量行为：

- `docker compose --env-file .env.development` 主要为 Compose 文件中的 `${VARIABLE}` 插值提供值；
- `environment` 明确把变量传入 Container。

因此，Compose 能读取 `.env.development`，不代表文件中的每个变量都会自动成为 API Container 的环境变量。需要传入 API 的配置仍应写在 `environment` 或 `env_file` 中。

这里暂时只传入 ASP.NET Core 环境名称。数据库 Connection String 会在创建 `DbContext` 时再加入，避免配置尚未使用的变量。

### 等待 PostgreSQL 健康

```yaml
depends_on:
  postgres:
    condition: service_healthy
```

`depends_on` 声明 API 依赖 `postgres` Service。

如果只写简短形式：

```yaml
depends_on:
  - postgres
```

Compose 只保证先启动 PostgreSQL Container，不保证 PostgreSQL 已经完成初始化并能接受连接。

使用：

```yaml
condition: service_healthy
```

Compose 会等待 `postgres` 的 Healthcheck 返回成功，再启动 API。这里等待的依据就是已经配置的：

```yaml
healthcheck:
  test:
    [
      "CMD-SHELL",
      "pg_isready -U $${POSTGRES_USER} -d $${POSTGRES_DB}"
    ]
```

因此，启动顺序更接近：

```text
启动 PostgreSQL Container
        ↓
反复执行 pg_isready
        ↓
PostgreSQL 状态变为 healthy
        ↓
启动 API Container
```

`service_healthy` 并不是自动理解 PostgreSQL 的业务状态，它只相信该 Service 自己定义的 Healthcheck。Healthcheck 检查得是否准确，决定了这个条件是否有意义。

`depends_on` 也不能取代应用自身的重试和错误处理。生产环境中，数据库可能在 API 启动后短暂重启或断开，API 仍然需要正确处理连接失败。

## 验证 Compose 配置

启动前先执行：

```bash
docker compose \
  --env-file .env.development \
  config \
  --quiet
```

这条命令会解析 Compose 文件、执行变量插值并验证最终配置，但不会构建 Image 或启动 Container。

`--quiet` 表示只返回验证结果，不输出展开后的完整配置。由于完整配置可能包含来自环境文件的密码，这种检查方式也能避免把数据库密码直接打印到终端。

如果命令没有输出且退出码为 `0`，说明 Compose 配置能够成功解析。但它只能证明配置结构有效，不能证明 Dockerfile 一定能成功构建，也不能证明 API 一定能够正常启动。

## 构建并启动完整开发环境

配置检查通过后执行：

```bash
docker compose \
  --env-file .env.development \
  up \
  --build
```

各部分含义如下：

| 参数 | 作用 |
|---|---|
| `--env-file .env.development` | 为 Compose 变量插值指定 Development 环境文件 |
| `up` | 创建并启动 Compose 中的 Service |
| `--build` | 启动前重新检查并构建需要构建的 Image |

第一次运行时先不要添加 `-d`。前台日志可以同时显示 Image Build、PostgreSQL Healthcheck 和 API 启动过程，更适合发现问题。

当日志中出现类似内容时，表示 ASP.NET Core 已开始监听 Container 内的 8080 端口：

```text
Now listening on: http://[::]:8080
```

然后访问 Controller 实际定义的路由，而不是只访问根路径 `/`。如果 API 没有定义根路由，`http://localhost:8080/` 返回 404 并不能说明 Container 启动失败。

完成验证后，可以按 `Ctrl+C` 停止前台运行，再执行：

```bash
docker compose --env-file .env.development down
```

该命令会停止并移除 Compose 创建的 Container 和默认 Network，但不会删除 Named Volume 中的 PostgreSQL 数据。只有明确加入 `--volumes` 才会移除 Compose 管理的 Volume，因此操作本地数据库时不要随意添加该参数。

## 小结

Dockerfile 与 Compose 解决的是不同层次的问题：

```text
Dockerfile
定义如何构建和运行一个 API Image
        ↓
Compose
定义 API 与 PostgreSQL 如何一起运行
```

这个多阶段 Dockerfile 的核心不是“把源码装进 Container”，而是建立一个可重复的构建流水线：

```text
使用 SDK Image 接收源码
        ↓
恢复依赖并发布应用
        ↓
开始独立的 Runtime Stage
        ↓
只复制发布结果
        ↓
以非 root 用户启动 API
```

需要记住的关键点是：

- `sdk:10.0` 负责构建，`aspnet:10.0` 负责运行；
- 先复制 `.csproj` 再 Restore，是为了更好地利用 Docker Build Cache；
- 缓存由 Docker Builder 提供，不是由 `RUN` 单独实现；
- 源码必须进入构建阶段，因为 `dotnet publish` 需要源码；
- 第二个 `FROM` 开始独立阶段，不会自动继承第一个阶段；
- `COPY --from=build` 只复制指定的发布目录；
- 最终 Image 不包含源码、SDK 和构建工具；
- `.dockerignore` 用于减少 Build Context，并防止无关或敏感文件进入构建过程；
- Compose 的 `build` 会使用 Dockerfile 构建 API Image；
- `ports` 负责把 API 的 Container 端口发布到开发机；
- `environment` 才会明确把配置传进 API Container；
- `depends_on: condition: service_healthy` 会等待 PostgreSQL Healthcheck 成功。

完成这一阶段后，下一步是创建数据库 Connection String、`AppDbContext` 和第一个 EF Core Migration，让 API 真正通过 Compose Network 访问 PostgreSQL。

## 参考资料

- [Docker multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [Dockerfile reference](https://docs.docker.com/reference/dockerfile/)
- [Docker Compose file reference](https://docs.docker.com/reference/compose-file/)
- [Control startup order with Compose](https://docs.docker.com/compose/how-tos/startup-order/)
- [.NET container images](https://learn.microsoft.com/dotnet/core/docker/container-images)
- [Containerize a .NET app with Docker](https://learn.microsoft.com/dotnet/core/docker/build-container)
- [.NET containers: non-root user and port 8080](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8/containers)
