---
title: "把 Docker 容器标准输出发送到 CloudWatch Logs"
description: "使用 Docker awslogs Logging Driver 集中收集前端、API 和 PostgreSQL 日志，并区分日志与主机指标。"
tags:
  - Docker
  - CloudWatch Logs
  - AWS IAM
  - Observability
---

# 把 Docker 容器标准输出发送到 CloudWatch Logs

应用已经通过 `ILogger` 把结构化事件写到标准输出。部署到单台 EC2 后，只看 `docker logs` 会依赖服务器当前状态；CloudWatch Logs 可以把多个容器的 stdout/stderr 集中保存和检索。

## 日志链路

```text
ILogger / Nginx / PostgreSQL
  ↓ stdout 或 stderr
Docker awslogs Logging Driver
  ↓ AWS API
CloudWatch Log Group
  ↓
Log Stream
```

这里没有修改业务 Logger。Docker 负责接住 Container 输出，再发送到 AWS。

## 为服务配置 awslogs

`compose.production.yaml` 中，每个服务加入：

```yaml
logging:
  driver: awslogs
  options:
    awslogs-region: ca-central-1
    awslogs-group: /blog-api/api
    awslogs-create-group: "true"
    awslogs-stream: api
```

PostgreSQL 和前端应使用各自的 Group 或 Stream，例如：

```text
/blog-api/postgres → postgres
/blog-api/api      → api
/blog-api/frontend → frontend
```

固定 Stream 适合单实例学习项目。多个 EC2 或多个副本共用同一个 Stream 会造成难以区分和写入竞争，届时应把 Instance ID 或 Container ID 纳入 Stream 名。

## IAM 权限

Docker Engine 在 EC2 Host 上调用 CloudWatch Logs，所以凭据来自 EC2 Instance Role。最小操作通常包括：

```json
{
  "Effect": "Allow",
  "Action": [
    "logs:CreateLogGroup",
    "logs:CreateLogStream",
    "logs:DescribeLogStreams",
    "logs:PutLogEvents"
  ],
  "Resource": "arn:aws:logs:REGION:ACCOUNT_ID:log-group:/blog-api/*"
}
```

如果预先创建 Log Group，可以去掉 `awslogs-create-group` 和 `logs:CreateLogGroup`。生产环境更适合预创建，并明确 Retention，防止日志无限增长。

不要在 Compose 中写 AWS Access Key。EC2 Role 的短期凭据会由 AWS Credential Provider Chain 获取。

## 为什么不是 CloudWatch Agent 配置文件

两者收集对象不同：

- `awslogs` Logging Driver：发送 Container stdout/stderr；
- CloudWatch Agent：采集主机内存、磁盘、CPU 细项和指定日志文件；
- EC2 默认 Metrics：提供基础 CPU、网络等指标，但不包含常用内存使用率。

因此类似 `mem_used_percent`、`disk used_percent` 的 JSON 是 CloudWatch Agent 配置，不放在 ASP.NET Core 的 `appsettings.json`，也不是本篇发送 Container Log 所必需。

## 启动前验证

```bash
docker compose \
  -f compose.production.yaml \
  --env-file .env.production \
  config --quiet
```

然后确认当前 EC2 Role 有写日志权限，再启动：

```bash
make prod-up
```

如果 Role 权限不足，Container 可能因为 Logging Driver 初始化失败而无法启动。先查 Docker Daemon 日志和 IAM，而不是修改应用代码。

## 在 CloudWatch 中验证

打开 CloudWatch → Logs → Log groups，依次确认三个 Group 有新事件。执行一次文章发布后，用结构化字段定位：

```text
PostId
UserId
TraceId
```

同一个 TraceId 应连接请求 Scope 与 Service 业务日志。不要把 JWT、密码、Connection String 或完整请求 Body 写入日志。

## 成本与保留策略

CloudWatch Logs 按写入、存储和查询等维度计费。学习项目也应为 Log Group 设置 Retention，例如 7 或 14 天，并避免在正常路径输出大量 Debug 日志。关闭 EC2 不会自动删除 Log Group。

## 常见问题

- Log Group 不存在：检查 `awslogs-create-group` 和 `logs:CreateLogGroup`。
- 容器启动时报 Credential 错误：检查 EC2 Instance Profile，而不是在服务器保存长期 Key。
- CloudWatch 有日志但没有内存指标：安装并配置 CloudWatch Agent。
- 日志能看到但字段不能方便检索：确认 Console Logger 含 Scope，并考虑后续改为 JSON Console Formatter。

## 参考资料

- [Docker awslogs logging driver](https://docs.docker.com/engine/logging/drivers/awslogs/)
- [CloudWatch Logs permissions reference](https://docs.aws.amazon.com/AmazonCloudWatch/latest/logs/permissions-reference-cwl.html)
- [CloudWatch Agent configuration](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-Agent-Configuration-File-Details.html)

## 主线导航

- 上一步：[使用 GitHub OIDC 和 SSM 手动部署 EC2](./24-github-oidc-ssm-ec2-deploy.md)
- 下一步：[保留 AWS 方案并部署到私人 VPS](./26-vps-deployment-profile.md)
