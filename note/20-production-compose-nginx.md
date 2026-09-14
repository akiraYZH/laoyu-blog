---
title: "使用 Docker Compose、Nginx 和健康检查运行生产栈"
description: "把 Vue 静态文件、ASP.NET Core API 和 PostgreSQL 组成只公开一个 HTTP 入口的生产式 Docker Compose 栈。"
tags:
  - Docker Compose
  - Nginx
  - ASP.NET Core
  - PostgreSQL
---

# 使用 Docker Compose、Nginx 和健康检查运行生产栈

开发环境里的 Vite Dev Server 和 `dotnet watch` 适合快速修改代码，却不是生产运行方式。本文建立独立的生产栈：Nginx 提供编译后的 Vue 文件并代理 API，ASP.NET Core 和 PostgreSQL 只留在 Compose 内部网络。

## 前置状态

开始前，项目应已经具备：

- Vue 前端可以通过相对路径 `/api/...` 调用后端；
- API 提供 `/health`，并能检查 PostgreSQL；
- API 与前端各自拥有可构建的 Dockerfile；
- Migration 已经提交到仓库。

## 最终请求路径

```text
Browser
  ↓ HTTP 80/443
Nginx Container
  ├─ /            → Vue dist 静态文件
  ├─ /api/...     → http://api:8080
  └─ /uploads/... → http://api:8080
                         ↓
                   PostgreSQL:5432
```

只发布 Nginx 的端口。`api:8080` 和 `postgres:5432` 是 Compose 网络内的服务地址，不需要暴露给公网。

## 构建前端运行镜像

`frontend/Dockerfile` 使用三个阶段：

```dockerfile
FROM node:24-alpine AS development
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .

FROM development AS build
RUN npm run build

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Node 只参与依赖安装和前端编译。最终镜像只包含 Nginx 与 `dist`，不会运行 Vite，也不需要携带前端源码和开发依赖。

## 配置 Nginx

`frontend/nginx.conf`：

```nginx
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location /api/ {
        proxy_pass http://api:8080;
    }

    location /uploads/ {
        proxy_pass http://api:8080;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

`try_files ... /index.html` 是 SPA 刷新不返回 404 的关键。`api` 不是域名，而是 Compose 自动提供的服务名 DNS。

## 建立生产 Compose

创建 `compose.production.yaml`，其中包括 `postgres`、`api` 和 `frontend`。数据库先定义健康检查：

```yaml
postgres:
  image: postgres:18-alpine
  restart: unless-stopped
  environment:
    POSTGRES_DB: ${POSTGRES_DB}
    POSTGRES_USER: ${POSTGRES_USER}
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  volumes:
    - postgres_data:/var/lib/postgresql
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U $${POSTGRES_USER} -d $${POSTGRES_DB}"]
    interval: 5s
    timeout: 5s
    retries: 5
    start_period: 10s
```

YAML 中的 `$${POSTGRES_USER}` 会把 `$` 留给 Container 内的 Shell；如果写成 `${POSTGRES_USER}`，Compose 会在启动 Container 前替换它。

API 等待数据库真正健康，而不是只等待数据库进程开始运行：

```yaml
api:
  build:
    context: .
    dockerfile: Dockerfile
    target: runtime
  restart: unless-stopped
  environment:
    ASPNETCORE_ENVIRONMENT: Production
    ConnectionStrings__BlogDb: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
  depends_on:
    postgres:
      condition: service_healthy
  healthcheck:
    test: ["CMD", "curl", "--fail", "--silent", "http://localhost:8080/health"]
```

普通的 `depends_on` 只保证依赖 Container 已启动；`condition: service_healthy` 才会等待它的 Healthcheck 成功。应用仍应能处理运行期间数据库短暂断开，启动顺序不能替代重试和错误处理。

前端只在 API 健康后启动，并且是唯一发布端口的服务：

```yaml
frontend:
  build:
    context: ./frontend
    dockerfile: Dockerfile
    target: runtime
  ports:
    - "${HTTP_PORT:-80}:80"
  depends_on:
    api:
      condition: service_healthy
```

## 配置与数据的边界

提交 `.env.example` 只用于说明变量名，真实值写入不提交 Git 的 `.env.production`：

```bash
cp .env.example .env.production
```

数据库与本地上传使用 Named Volume：

```yaml
volumes:
  postgres_data:
  uploads_data:
```

重新创建 Container 不会删除 Named Volume。`docker compose down -v` 会删除 Volume，应只在明确需要重置数据时执行。

## 运行和验证

```bash
docker compose \
  -p blog-api-prod \
  -f compose.production.yaml \
  --env-file .env.production \
  config --quiet

docker compose \
  -p blog-api-prod \
  -f compose.production.yaml \
  --env-file .env.production \
  up --detach --build
```

然后检查：

```bash
docker compose -p blog-api-prod -f compose.production.yaml ps
curl --fail http://localhost/health
```

预期三个服务最终都为 Running/Healthy，浏览器可以刷新 Vue 的子路由，PostgreSQL 端口不能从公网直接访问。

## 常见问题

- 前端首页可访问但刷新子路由 404：检查 `try_files`。
- API 连接数据库失败：Connection String 的 Host 应使用 Compose 服务名 `postgres`，不是 `localhost`。
- Container 一直 Unhealthy：先执行 `docker compose logs api`，Healthcheck 失败只是结果，不是根因。
- 停止后数据消失：检查是否使用了 `down -v`，以及 Volume 是否挂到数据库真实数据目录。

## 参考资料

- [Docker Compose startup order](https://docs.docker.com/compose/how-tos/startup-order/)
- [Docker Compose services](https://docs.docker.com/reference/compose-file/services/)
- [Nginx reverse proxy](https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy/)

## 主线导航

- 上一步：[使用日志 Scope 关联用户、请求与文章操作](./19-structured-logging-request-scope.md)
- 下一步：[通过配置切换本地与 S3 图片存储](./21-switch-local-s3-image-storage.md)
