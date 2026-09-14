---
title: "使用 GitHub OIDC 和 SSM 手动部署 EC2"
description: "让 GitHub Actions 通过短期身份承担 Deploy Role，再由 Systems Manager 在 EC2 上执行部署命令。"
tags:
  - GitHub Actions
  - AWS OIDC
  - AWS IAM
  - AWS Systems Manager
---

# 使用 GitHub OIDC 和 SSM 手动部署 EC2

直接把长期 AWS Access Key 或 SSH Private Key 放进 GitHub Secrets 会扩大凭据泄露风险。本文使用两段临时授权：

```text
GitHub Actions
  ↓ OIDC Token
AWS STS AssumeRoleWithWebIdentity
  ↓ 临时 AWS Credentials
SSM SendCommand
  ↓
EC2 上的 SSM Agent
  ↓
git pull + make prod-up + make prod-ps
```

Workflow 只在人工点击 Run workflow 后执行。

## 两个 Role 不要混淆

### GitHub Deploy Role

这个 Role 被 GitHub Actions 承担，权限只需要：

- 向指定 EC2 发送 SSM Command；
- 查询这条 Command 的执行结果。

它的 Trust Policy 信任 GitHub OIDC Provider，并限制仓库和分支。示意条件：

```json
{
  "Condition": {
    "StringEquals": {
      "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
    },
    "StringLike": {
      "token.actions.githubusercontent.com:sub":
        "repo:OWNER/REPOSITORY:ref:refs/heads/main"
    }
  }
}
```

`sub` 不应使用允许所有仓库和所有分支的通配符。Role 的 Permission Policy 也应限制目标 Instance。

### EC2 Instance Role

这个 Role 附加在 EC2 上，供服务器内的程序使用。它通常包含：

- `AmazonSSMManagedInstanceCore`，让 SSM Agent 注册并接收命令；
- 应用访问指定 S3 Prefix 的最小权限；
- 容器向指定 CloudWatch Log Group 写入的权限。

GitHub Role 负责“命令哪台服务器部署”；EC2 Role 负责“服务器自身可以访问哪些 AWS 服务”。

## 为什么 OIDC 可以换取 Role

GitHub 为当前 Workflow 生成一个短期、可验证的 OIDC Token。Token 带有 Repository、Branch、Audience 等 Claims。AWS IAM 检查 Trust Policy：

```text
签名可信 + aud 匹配 + sub 匹配
  ↓
STS 返回短期 Access Key、Secret 和 Session Token
```

AWS 没有因为“请求来自 GitHub”就自动放行；真正的授权依据是预先配置的 OIDC Provider、Role Trust Policy 和 Claims 条件。

## 手动触发 Workflow

`.github/workflows/deploy.yml`：

```yaml
name: Deploy

on:
  workflow_dispatch:

permissions:
  contents: read

jobs:
  deploy:
    if: github.ref == 'refs/heads/main'
    permissions:
      id-token: write
      contents: read
```

`id-token: write` 允许 Workflow 请求 OIDC Token，不代表它拥有仓库写权限。`if` 再次确保只部署 Main Branch。

## 承担 AWS Role

```yaml
- name: Configure AWS credentials
  uses: aws-actions/configure-aws-credentials@v6
  with:
    role-to-assume: ${{ vars.AWS_ROLE_ARN }}
    role-session-name: blog-api-${{ github.run_id }}
    aws-region: ${{ vars.AWS_REGION }}
    mask-aws-account-id: true
```

`AWS_ROLE_ARN`、`AWS_REGION` 和 `EC2_INSTANCE_ID` 不是密码，可以放 Repository Variables；密码、Token 和私钥才放 Secrets。

如果这里出现 `Not authorized to perform sts:AssumeRoleWithWebIdentity`，重点检查 Role 的 Trust Policy，而不是给 Workflow 增加管理员权限。

## 通过 SSM 执行部署

```bash
aws ssm send-command \
  --instance-ids "$EC2_INSTANCE_ID" \
  --document-name "AWS-RunShellScript" \
  --timeout-seconds 1800 \
  --parameters '{
    "commands":[
      "sudo -u ubuntu git -C /home/ubuntu/apps/blog-api pull --ff-only origin main",
      "sudo -u ubuntu make -C /home/ubuntu/apps/blog-api prod-up",
      "sudo -u ubuntu make -C /home/ubuntu/apps/blog-api prod-ps"
    ]
  }'
```

`--ff-only` 避免服务器工作目录产生自动 Merge Commit。生产环境文件保留在服务器且不提交 Git。

`send-command` 只表示命令已经进入队列，不能证明部署完成。Workflow 需要保存 `CommandId`，循环调用 `get-command-invocation`，直到得到 `Success`、`Failed`、`Cancelled` 或 `TimedOut`。

## SSM 成功工作的条件

EC2 必须同时满足：

1. SSM Agent 正在运行；
2. EC2 附加了正确的 Instance Profile；
3. 实例能访问 SSM Endpoint；
4. Fleet Manager 中显示为 Managed Node；
5. GitHub Deploy Role 有权发送并查询命令。

SSM 允许关闭公网 SSH 入口，但它不是免费的“超级权限”。命令仍以实例上的系统用户运行，文件权限和 `sudo` 规则仍然生效。

## 验证

在 GitHub Actions 手动选择 Main Branch 运行 Deploy。成功结果至少包括：

- OIDC 步骤没有长期 Access Key；
- SSM 返回 Command ID；
- 最终 Status 为 `Success`；
- `prod-ps` 显示数据库与 API Healthy、前端 Running；
- 公网 `/health` 和一条只读文章请求成功。

## 回滚边界

当前 Workflow 是服务器端 `git pull` 后重建，没有自动回滚。正式使用前应记录上一个可用 Commit，并设计明确的回滚命令。数据库 Migration 可能不可逆，不能只回退 Git 就假设数据库自动恢复。

## 参考资料

- [GitHub Actions OIDC in AWS](https://docs.github.com/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-amazon-web-services)
- [AWS IAM OIDC federation](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_create_oidc.html)
- [AWS Systems Manager Run Command](https://docs.aws.amazon.com/systems-manager/latest/userguide/run-command.html)

## 主线导航

- 上一步：[使用 Husky 在提交前检查前后端](./23-husky-pre-commit-fullstack.md)
- 下一步：[把容器标准输出发送到 CloudWatch Logs](./25-docker-cloudwatch-logs.md)
