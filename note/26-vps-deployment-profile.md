---
title: "保留 AWS 方案并部署到私人 VPS"
description: "通过独立 Compose 与手动 GitHub Actions Workflow，让 VPS 使用本地图片 Volume，同时保留 AWS 展示配置。"
tags:
  - VPS
  - Docker Compose
  - GitHub Actions
  - Deployment
---

# 保留 AWS 方案并部署到私人 VPS

项目不必为了切换服务器而删除 AWS 代码。更清晰的做法是保留一套应用代码和两套部署 Profile：

```text
AWS Profile → compose.production.yaml → S3 + CloudWatch + EC2 Role
VPS Profile → compose.vps.yaml        → Local Volume + Docker Local Logs
```

配置决定基础设施行为，Controller、Service Contract 和前端 API 路由保持不变。

## 为什么不在一个 Compose 中堆条件

Compose 不擅长大量 if/else。两个文件可以直观看出每个环境使用了什么资源，也避免普通 VPS 因为没有 AWS Credential 而无法启动。

共同部分仍然一致：

- PostgreSQL 使用 Named Volume；
- API 和数据库不发布端口；
- Nginx 是唯一 HTTP 入口；
- API 等待数据库健康；
- 前端等待 API 健康；
- Secret 从服务器的 `.env.production` 读取。

## VPS Compose 的关键差异

`compose.vps.yaml` 不配置 `awslogs`，图片 Provider 由环境变量选择：

```yaml
api:
  environment:
    ASPNETCORE_ENVIRONMENT: Production
    Storage__Provider: ${STORAGE_PROVIDER:-Local}
  volumes:
    - uploads_data:/app/wwwroot/uploads
```

VPS 的 `.env.production`：

```text
STORAGE_PROVIDER=Local
HTTP_PORT=80
```

S3 变量可以留在 `.env.example` 说明 AWS Profile 的需求，但 VPS 的应用启动不应要求它们有真实值。

## 用 Makefile 固定命令

```make
VPS_COMPOSE = docker compose \
	-p $(PROD_PROJECT) \
	-f compose.vps.yaml \
	--env-file $(PROD_ENV_FILE)

vps-up:
	$(VPS_COMPOSE) up --detach --build
```

在服务器上执行：

```bash
make vps-up
make vps-ps
make vps-logs
```

命令名明确指出正在操作 VPS Profile，降低误用 AWS Compose 的概率。

## 手动 GitHub Actions 部署

`.github/workflows/deploy-vps.yml` 使用：

```yaml
on:
  workflow_dispatch:

jobs:
  deploy:
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          port: ${{ secrets.VPS_PORT }}
          script: |
            cd /var/www/blog-api
            git pull --ff-only origin main
            make vps-up
```

Host、User、Private Key 和 Port 放在 GitHub Secrets。Workflow 仍是手动触发，因此 Merge 到 Main 只运行 CI，不会自动部署。

示例目录要替换为服务器真实路径。SSH 用户只应拥有部署所需权限，不应直接使用 Root。

## 宝塔面板的职责

宝塔可以负责域名、TLS 证书和外层反向代理：

```text
Internet HTTPS
  ↓
宝塔 Nginx / Reverse Proxy
  ↓ localhost:HTTP_PORT
Compose Frontend Nginx
```

不要同时让宝塔和 Compose 争用 Host 的 80 端口。若宝塔占用 80/443，可以把 `HTTP_PORT` 设置为只供本机代理的高位端口，并让防火墙不对公网开放该端口。

## 数据持久化与备份

`postgres_data` 和 `uploads_data` 只解决“重建 Container 后数据仍在”，不等于备份。至少定期保存：

- `pg_dump` 的 PostgreSQL 备份；
- 上传 Volume 或其宿主机备份；
- `.env.production` 的安全离线副本。

还应实际做一次恢复演练。没有验证恢复的备份只能算“可能可用”。

## 停用 AWS 后仍需处理的资源

停止 EC2 不等于不再收费。若 AWS 方案只用于代码展示，应检查并按需删除或停用：

- EC2 Instance；
- EBS Volume 与 Snapshot；
- Elastic IP/Public IPv4；
- S3 Object 与 Bucket；
- CloudWatch Log Group；
- 预算告警外的其他收费资源。

IAM Role 和 OIDC Provider 本身通常不是主要费用来源，可以保留架构示例，但权限不再需要时应禁用或删除。

## 验证

部署完成后按顺序验证：

```bash
make vps-ps
curl --fail http://127.0.0.1:<HTTP_PORT>/health
curl --fail https://example.com/health
```

再完成一次登录、上传图片、重建 API Container 和重新读取图片。最后确认 5432 没有向公网开放。

## 参考资料

- [Docker volumes](https://docs.docker.com/engine/storage/volumes/)
- [GitHub Actions secrets](https://docs.github.com/actions/security-guides/using-secrets-in-github-actions)
- [Nginx reverse proxy](https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy/)

## 主线导航

- 上一步：[把 Docker 容器标准输出发送到 CloudWatch Logs](./25-docker-cloudwatch-logs.md)
- 下一步：[使用 PostgreSQL 数组保存并筛选文章标签](./27-postgresql-array-tags.md)
