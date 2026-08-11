---
title: "使用 Docker Compose 启动 PostgreSQL 本地开发数据库"
description: "用环境变量、端口映射、Named Volume 和 Healthcheck 创建可复现且可验证的 PostgreSQL 开发环境。"
tags:
  - PostgreSQL
  - Docker Compose
  - Development
---

# 使用 Docker Compose 启动 PostgreSQL 本地开发数据库

本文只解决一个问题：用 Docker Compose 启动一个重启后数据仍然存在、并且可以确认健康状态的 PostgreSQL 开发数据库。

## 准备环境变量

创建 `.env.development`：

```dotenv
POSTGRES_DB=blog_dev
POSTGRES_USER=blog_user
POSTGRES_PASSWORD=replace_with_local_password
```

这个文件包含本地凭据，不应提交到 Git：

```gitignore
.env
.env.*
!.env.example
```

可以提交不含真实密码的 `.env.example`，用于说明项目需要哪些变量。

## Compose 配置

创建 `compose.yaml`：

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
        - CMD-SHELL
        - pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  postgres_data:
```

## Service 与 Image

```yaml
services:
  postgres:
    image: postgres:18-alpine
```

`postgres` 是 Compose Service Name，可用于命令和 Compose Network DNS。

`postgres:18-alpine` 固定 PostgreSQL Major Version，并使用较小的 Alpine-based Image。不要在未检查兼容性的情况下使用漂移的 `latest` Tag。

## 初始化变量

官方 PostgreSQL Image 在第一次初始化空数据目录时读取：

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
```

如果 Named Volume 已经包含数据库，这些变量不会重新创建 User 或覆盖密码。修改 `.env.development` 不等于修改现有数据库内部状态。

## 端口映射

```yaml
ports:
  - "5432:5432"
```

格式是：

```text
开发机端口:Container 端口
```

因此开发机工具使用：

```text
Host=localhost
Port=5432
```

如果本机 5432 已被占用，可以映射为：

```yaml
ports:
  - "5433:5432"
```

此时开发机使用 5433，Container 内部 PostgreSQL 仍监听 5432。

## Named Volume

```yaml
volumes:
  - postgres_data:/var/lib/postgresql
```

PostgreSQL 数据写入 Docker 管理的 Named Volume。Container 可以删除和重建，而数据继续保留。

PostgreSQL 18+ Official Image 使用 Major-version-specific Data Directory，推荐把 Volume 挂载到 `/var/lib/postgresql`。PostgreSQL 17 及更早版本的常见挂载目标是 `/var/lib/postgresql/data`，升级 Major Version 时不能只替换 Image Tag，仍需要正确的数据库升级流程。

```text
Container → 可重建的进程和文件系统
Volume    → 持久化数据库数据
```

常用区别：

```bash
docker compose down
```

停止并删除 Container，但保留 Named Volume。

```bash
docker compose down --volumes
```

同时删除 Volume，数据库数据将丢失。只在明确需要重置开发数据时使用。

## Healthcheck

Container 处于 Running 不代表 PostgreSQL 已经可以接受连接。

```yaml
healthcheck:
  test:
    - CMD-SHELL
    - pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}
```

`pg_isready` 检查 PostgreSQL 是否已经响应连接请求。Compose 会把结果标记为 `healthy` 或 `unhealthy`。

## 启动前验证配置

```bash
docker compose \
  --env-file .env.development \
  config --quiet
```

这可以发现 YAML、变量插值和结构错误，但不会启动 Container。

## 启动并检查

```bash
docker compose \
  --env-file .env.development \
  up --detach
```

查看状态：

```bash
docker compose ps
```

预期 PostgreSQL 最终显示 `healthy`。

查看日志：

```bash
docker compose logs postgres --tail 50
```

## 使用 psql 验证

```bash
docker compose \
  --env-file .env.development \
  exec postgres \
  psql -U blog_user -d blog_dev \
  -c "SELECT current_database(), current_user;"
```

预期返回配置的 Database 和 User。这个查询比只看 Container Running 更能证明数据库可连接。

## 使用图形化工具

VS Code PostgreSQL Extension、DBeaver 或 pgAdmin 可以使用：

```text
Host: localhost
Port: 5432
Database: blog_dev
Username: blog_user
Password: 本地环境文件中的值
```

图形化工具只是客户端，不会替代 PostgreSQL Server、Volume 或 Migration。

## 总结

```text
.env.development → 本地初始化参数
Compose Service  → 运行 PostgreSQL
Port Mapping     → 允许开发机连接
Named Volume     → 保存数据
Healthcheck      → 判断数据库是否就绪
psql             → 验证真实连接
```

## 参考资料

- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [Docker Compose](https://docs.docker.com/compose/)
- [Docker volumes](https://docs.docker.com/engine/storage/volumes/)

## 主线导航

- 上一步：[安装 EF Core PostgreSQL 工具链](./01a-install-ef-core-postgresql-tools.md)
- 下一步：[配置 EF Core DbContext](./04-aspnet-core-ef-core-postgresql-dbcontext-migrations.md)
