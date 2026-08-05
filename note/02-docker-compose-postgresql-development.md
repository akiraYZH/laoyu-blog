---
title: "使用 Docker Compose 搭建 PostgreSQL 18 本地开发环境"
description: "为 ASP.NET Core API 准备可复现的 PostgreSQL 18 开发数据库：环境变量、端口映射、Named Volume、健康检查、启动验证和图形化连接。"
date: 2026-07-31
status: draft
tags:
  - Docker Compose
  - PostgreSQL 18
  - ASP.NET Core
  - EF Core
  - Development
---

# 使用 Docker Compose 搭建 PostgreSQL 18 本地开发环境

在 ASP.NET Core 项目中安装 Npgsql 和 EF Core，只是让应用具备访问 PostgreSQL 的能力。真正保存数据的 PostgreSQL Server 仍然需要单独运行。

本地开发可以直接在操作系统中安装 PostgreSQL，也可以连接云数据库。本文选择 Docker Compose，目标是得到一个可重复创建、可持久化、能够判断健康状态的 PostgreSQL 18 开发环境。

最终结构如下：

```text
ASP.NET Core API（暂时运行在开发机）
        ↓ localhost:5432
Docker 端口映射
        ↓
PostgreSQL 18 Container
        ↓
Named Volume
```

完成本文后，可以确认：

- PostgreSQL 18 Container 正常运行；
- 开发数据库和数据库用户已创建；
- 容器状态达到 `healthy`；
- 数据保存在 Docker Named Volume；
- 命令行和 VS Code 都能连接数据库；
- 数据库密码不会写进 Compose 文件或提交到仓库。

## 为什么本地开发使用 Docker Compose

直接安装 PostgreSQL 没有问题，但 Docker Compose 更适合需要重复搭建环境的项目：

- 固定 PostgreSQL 主版本；
- 不把数据库服务直接安装进开发机；
- 一条命令创建 Network、Volume 和 Container；
- 团队成员可以使用相同配置；
- 删除和重建 Container 不必删除数据库数据；
- 后续可以继续加入 ASP.NET Core API Service；
- 本地结构能够自然过渡到单台服务器上的 Docker Compose。

Docker 并不是数据库驱动。组件职责仍然是：

```text
Npgsql Provider
    → 让 EF Core 能够连接 PostgreSQL

PostgreSQL Server
    → 执行 SQL 并保存数据

Docker Compose
    → 定义和运行 PostgreSQL Container
```

## 开始前的环境检查

确认 Docker Client、Docker Engine 和 Docker Compose 都能使用：

```bash
docker version
docker compose version
```

`docker version` 应同时显示 `Client` 和 `Server`。如果只有 Client，通常表示 Docker Desktop 尚未启动，或者当前用户无法连接 Docker Engine。

还应确认 PostgreSQL 默认端口没有被其他程序占用：

```bash
lsof -nP -iTCP:5432 -sTCP:LISTEN
```

没有输出通常表示端口可用。如果已经有本机 PostgreSQL 或其他 Container 占用 `5432`，可以先停止它，或者为新 Container 选择不同的宿主机端口。

## 创建 Compose 文件

在项目根目录创建：

```text
compose.yaml
```

写入完整 Development 配置：

```yaml
services:
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

下面逐部分说明这份配置。

## `services` 与 Service 名称

```yaml
services:
  postgres:
```

`services` 是 Compose 的顶层配置，表示当前应用需要运行哪些服务。

`postgres` 是 Service 的逻辑名称。它不是数据库名，也不是用户或表名。Compose 命令会使用这个名称：

```bash
docker compose logs postgres
docker compose stop postgres
docker compose exec postgres ...
```

以后如果 ASP.NET Core API 也加入 Compose，它可以在内部网络中使用：

```text
Host=postgres
```

连接数据库，因为 Compose 默认会为 Service 提供内部 DNS 名称。

## 固定 PostgreSQL Image

```yaml
image: postgres:18-alpine
```

其中：

- `postgres` 是 PostgreSQL 官方 Image；
- `18` 固定 PostgreSQL 主版本；
- `alpine` 使用体积较小的 Alpine Linux 变体。

不要只依赖：

```yaml
image: postgres
```

它通常指向浮动的 `latest` Tag，未来重新拉取时可能得到不同的大版本。明确固定主版本更利于复现环境。

## 使用环境变量初始化数据库

```yaml
environment:
  POSTGRES_DB: ${POSTGRES_DB}
  POSTGRES_USER: ${POSTGRES_USER}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

左边是传入 PostgreSQL Container 的变量名，右边是 Compose 在启动前读取的变量：

```text
Compose 外部变量
    ↓
${POSTGRES_DB}
    ↓
Container 内的 POSTGRES_DB
```

PostgreSQL 官方 Image 第一次发现数据目录为空时，会使用这些变量：

1. 初始化数据库集群；
2. 创建数据库用户；
3. 设置用户密码；
4. 创建指定数据库。

一个容易忽略的细节是：这些变量主要用于第一次初始化。如果 Named Volume 中已经存在数据库，再修改 `.env.development` 不会自动修改已有数据库用户的密码。

## 把 Development 值放进独立文件

在项目根目录创建：

```text
.env.development
```

示例内容：

```dotenv
POSTGRES_DB=blog_dev
POSTGRES_USER=blog_user
POSTGRES_PASSWORD=blog_dev_password
```

格式是：

```text
变量名=值
```

等号两侧不加空格，每行一个变量。

这里使用的是可丢弃的本地开发密码。Production 不应该复制这个值，也不应该把真实密码直接写进 Compose 文件。

## 防止环境文件进入仓库

`.gitignore` 至少加入：

```gitignore
# .NET build output
bin/
obj/

# Local environment variables and secrets
.env
.env.*
!.env.example
```

规则含义是：

- 忽略 `.env`；
- 忽略 `.env.development`、`.env.production` 等文件；
- 允许提交不含真实密码的 `.env.example`；
- 忽略可以重新生成的 .NET 构建目录。

可以另外创建 `.env.example`：

```dotenv
POSTGRES_DB=your_database
POSTGRES_USER=your_user
POSTGRES_PASSWORD=your_password
```

它只说明项目需要哪些变量，不保存真实凭据。

## Development 端口映射

```yaml
ports:
  - "5432:5432"
```

短格式是：

```text
宿主机端口:容器端口
```

因此开发机上的 ASP.NET Core 和数据库扩展可以访问：

```text
localhost:5432
```

然后 Docker 把连接转发到 Container 内的 PostgreSQL `5432`。

不指定宿主机地址时，Docker 通常绑定所有接口：

```text
0.0.0.0:5432
```

如果只需要本机访问，更严格的写法是：

```yaml
ports:
  - "127.0.0.1:5432:5432"
```

这不是功能上的必要条件，但可以避免开发数据库监听所有宿主机网络接口。在公司、酒店或公共网络中更值得采用。

Production 通常不应该把 PostgreSQL `5432` 发布到公网。如果 API 与 PostgreSQL 位于同一 Compose Network，API 可以直接通过 `postgres:5432` 连接，不需要宿主机端口映射。

## 使用 Named Volume 持久化数据

Service 内部引用 Volume：

```yaml
volumes:
  - postgres_data:/var/lib/postgresql
```

文件底部声明同一个 Volume：

```yaml
volumes:
  postgres_data:
```

这两个 `postgres_data` 不是两份数据，而是“声明”和“引用”使用同一个逻辑名称。

短格式：

```text
postgres_data:/var/lib/postgresql
```

等价于：

```yaml
volumes:
  - type: volume
    source: postgres_data
    target: /var/lib/postgresql
```

其中：

- `source` 是 Docker Named Volume；
- `target` 是 PostgreSQL Container 内的数据目录。

实际创建的 Volume 通常会自动带 Compose Project 前缀：

```text
<compose-project>_postgres_data
```

可以运行：

```bash
docker volume ls
```

查看实际名称。

删除 Container 不等于删除 Volume：

```bash
docker compose --env-file .env.development down
```

默认保留数据库数据。

下面的命令会连 Volume 一起删除：

```bash
docker compose --env-file .env.development down -v
```

除非确定要重置本地数据库，否则不要随意使用 `-v`。

## 用健康检查判断数据库是否可连接

Container 显示 `running`，只代表主进程已经启动，不代表 PostgreSQL 已准备好接受连接。

健康检查使用 PostgreSQL 自带的 `pg_isready`：

```yaml
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
```

启动状态可能经历：

```text
Container started
    ↓
PostgreSQL 初始化数据目录
    ↓
创建用户和数据库
    ↓
pg_isready 成功
    ↓
Container healthy
```

参数含义：

- `interval: 5s`：每 5 秒检查一次；
- `timeout: 5s`：一次检查最多等待 5 秒；
- `retries: 5`：连续失败 5 次后标记为 `unhealthy`；
- `start_period: 10s`：启动初期提供 10 秒初始化宽限期。

命令中使用：

```text
$${POSTGRES_USER}
```

双美元符号告诉 Compose 不要在宿主机提前替换，而是把 `${POSTGRES_USER}` 传入 Container，再由 Container Shell 使用自己的环境变量展开。

Docker 官方的 PostgreSQL Compose 示例同样使用 Named Volume 和 `pg_isready` 健康检查。[Docker PostgreSQL 指南](https://docs.docker.com/guides/postgresql/)

## 启动前验证配置

先让 Compose 读取变量并检查最终配置：

```bash
docker compose \
  --env-file .env.development \
  config --quiet
```

`config` 会解析 Compose 文件并执行变量替换，`--quiet` 只返回成功或失败，不把包含密码的最终配置打印到终端。

退出码为 `0` 且没有输出，表示配置可以被 Compose 正确解析。

这个命令不会：

- 下载 Image；
- 创建 Container；
- 创建 Volume；
- 占用端口。

## 第一次启动 PostgreSQL

运行：

```bash
docker compose \
  --env-file .env.development \
  up -d
```

第一次执行会完成：

1. 拉取 `postgres:18-alpine`；
2. 创建默认 Compose Network；
3. 创建 `postgres_data` Named Volume；
4. 创建 PostgreSQL Container；
5. 初始化数据库；
6. 启动健康检查。

`-d` 是 `--detach` 的缩写，让 Container 在后台运行，不持续占用当前终端。

## 检查健康状态

运行：

```bash
docker compose \
  --env-file .env.development \
  ps
```

正常状态类似：

```text
SERVICE    STATUS
postgres   Up ... (healthy)
```

如果暂时显示：

```text
health: starting
```

通常只是 PostgreSQL 正在初始化。等待几秒后再次执行 `ps`。

需要查看启动过程时，可以读取日志：

```bash
docker compose \
  --env-file .env.development \
  logs postgres
```

## 使用 psql 验证数据库和用户

`healthy` 之后，再执行一条只读 SQL：

```bash
docker compose \
  --env-file .env.development \
  exec postgres \
  psql -U blog_user -d blog_dev \
  -c "SELECT current_database(), current_user;"
```

其中：

- `exec postgres`：在已经运行的 PostgreSQL Service 中执行命令；
- `psql`：PostgreSQL 官方命令行客户端；
- `-U blog_user`：指定数据库用户；
- `-d blog_dev`：指定数据库；
- `-c`：执行一条 SQL 后退出。

预期结果：

```text
 current_database | current_user
------------------+-------------
 blog_dev         | blog_user
(1 row)
```

这个结果比单独看到 `healthy` 更完整地证明：

- 目标数据库存在；
- 目标用户存在；
- 用户能够连接数据库；
- SQL 可以正常执行。

`psql` 的 `-d`、`-U` 和 `-c` 参数定义可参考 [PostgreSQL 18 psql 文档](https://www.postgresql.org/docs/18/app-psql.html)。

## 使用 VS Code 图形化浏览 PostgreSQL

命令行验证完成后，可以使用 VS Code PostgreSQL 扩展连接：

```text
Host: localhost
Port: 5432
Database: blog_dev
Username: blog_user
Password: blog_dev_password
```

图形化工具适合：

- 浏览数据库和表；
- 查看字段和约束；
- 执行 SQL；
- 查看查询结果。

Docker Desktop 主要展示 Container、Image、Volume、日志和资源状态，不等同于数据库管理界面。

## 为什么暂时不把 API 放进 Compose

API 也可以成为 Compose Service：

```text
Browser
    ↓ localhost:8080
ASP.NET Core Container
    ↓ postgres:5432
PostgreSQL Container
```

这样 `.env.development` 可以同时为 API 和 PostgreSQL 提供变量，启动命令也可以统一为一次 `docker compose up`。

但在数据库接入的第一阶段，先让 API 运行在开发机上更容易排查问题：

- VS Code 断点调试简单；
- `dotnet watch` 可以直接热重载；
- `dotnet-ef` Local Tool 可以直接运行；
- 数据库连接错误不会和 Dockerfile、Image Build、Container Network 混在一起。

推荐顺序是：

```text
先让 PostgreSQL 在 Compose 中运行
    ↓
在开发机上完成 DbContext、Migration 和 CRUD
    ↓
证明数据库代码正确
    ↓
再为 API 添加 Dockerfile 和 Compose Service
```

容器化 API 后，连接字符串中的 Host 必须从：

```text
localhost
```

改成 Compose Service 名：

```text
postgres
```

因为 API Container 中的 `localhost` 指向 API Container 自己，而不是 PostgreSQL Container。

## Development 与 Production 如何分开

开发和生产可以使用相同的 PostgreSQL 主版本，但配置重点不同：

| 配置 | Development | Production |
|---|---|---|
| Image | 固定 PostgreSQL 18 | 固定兼容的 PostgreSQL 18 |
| 端口 | 可以映射到开发机 | 不向公网开放 5432 |
| 密码 | 本地 Secret 文件 | Secret Manager 或服务器环境变量 |
| 存储 | Docker Named Volume | EBS、备份或托管数据库 |
| 数据 | 可以重建 | 必须保护和备份 |
| API Host | `localhost` | Compose 中为 `postgres`，RDS 时为数据库地址 |

当前 `compose.yaml` 只用于 Development。未来部署时应创建独立的 Production 配置，不能直接把本地密码和开放端口复制到服务器。

## 当前技术检查点

目前已经完成：

- 安装 PostgreSQL EF Core Provider；
- 安装 EF Core Design 包；
- 配置项目级 `dotnet-ef`；
- 创建 Development Compose 配置；
- 使用独立 `.env.development` 提供数据库初始化变量；
- 用 `.gitignore` 排除本地环境文件；
- 配置 PostgreSQL 18 Image；
- 映射开发端口；
- 配置 Named Volume；
- 配置 `pg_isready` 健康检查；
- 启动并确认 Container 为 `healthy`；
- 使用 `psql` 验证 `blog_dev` 和 `blog_user`；
- 使用 VS Code PostgreSQL 扩展连接数据库。

尚未实施：

- ASP.NET Core Development Connection String；
- `AppDbContext`；
- Blog Entity；
- 依赖注入中的 `AddDbContext` 和 `UseNpgsql`；
- 第一个 EF Core Migration；
- PostgreSQL 数据表；
- Controller 的真实数据库查询；
- ASP.NET Core API Container。

下一步是在 ASP.NET Core Development 环境中安全保存连接字符串，然后创建 `DbContext`。在 API 暂时运行于开发机的阶段，可以使用 .NET User Secrets；等 API 进入 Compose 后，再改用 Container Environment Variables。

## 参考资料

- [Docker Compose](https://docs.docker.com/compose/)
- [Docker PostgreSQL Guide](https://docs.docker.com/guides/postgresql/)
- [Docker Volumes](https://docs.docker.com/engine/storage/volumes/)
- [PostgreSQL 18 psql](https://www.postgresql.org/docs/18/app-psql.html)
- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)

