---
title: "使用 Husky 在提交前检查前后端"
description: "在同一个 Git Hook 中运行前端 staged-file 检查和后端 xUnit 测试，并说明它与 CI 的职责边界。"
tags:
  - Husky
  - Git Hooks
  - lint-staged
  - xUnit
---

# 使用 Husky 在提交前检查前后端

CI 会在代码 Push 后发现问题，Pre-commit Hook 则在创建 Commit 前提供更快的本地反馈。Husky 是由 Node 安装的 Git Hook 管理工具，但 Hook 可以运行任何命令，因此不只适用于前端。

## 安装位置

本项目已有 `frontend/package.json`，所以把 Husky 和 lint-staged 放在前端 Dev Dependencies：

```json
{
  "scripts": {
    "prepare": "cd .. && husky frontend/.husky",
    "lint:staged": "lint-staged"
  },
  "devDependencies": {
    "husky": "^9.1.7",
    "lint-staged": "^16.4.0"
  }
}
```

`prepare` 在安装依赖后，从仓库根目录把 Git 的 Hooks Path 指向 `frontend/.husky`。

首次安装或 Clone 后执行：

```bash
npm --prefix frontend install
```

验证 Git 是否找到 Hook：

```bash
git config --get core.hooksPath
```

预期输出 `frontend/.husky/_` 或由当前 Husky 版本管理的等价路径。

## 只格式化本次提交涉及的前端文件

`frontend/package.json` 中的 lint-staged 配置：

```json
{
  "lint-staged": {
    "*.{js,jsx,ts,tsx,vue}": [
      "oxlint --fix",
      "eslint --fix --cache",
      "prettier --write"
    ],
    "*.{css,json,md,html,yml,yaml}": "prettier --write"
  }
}
```

lint-staged 只处理 Git 暂存区匹配的文件，适合快速修复格式和 Lint。它不会验证整个项目能否 Build。

## 在同一个 Hook 检查后端

`frontend/.husky/pre-commit`：

```sh
npm --prefix frontend run lint:staged
dotnet test tests/BlogApi.Tests/BlogApi.Tests.csproj
```

Git 从仓库根目录执行 Hook，所以测试项目路径也从根目录计算。任意命令返回非零 Exit Code，Commit 就会停止。

数据流是：

```text
git commit
  ↓
lint-staged 修复暂存的前端文件
  ↓
dotnet test 验证后端
  ↓
全部成功 → 创建 Commit
```

## 为什么不能只靠 Husky

本地 Hook 可以被 `--no-verify` 绕过，也可能因为依赖未安装而没有启用。CI 在 GitHub Runner 中重新检查整个仓库，才是团队共享的验证结果。

合理分工：

- Husky：尽早反馈，减少明显错误进入 Commit；
- CI：在统一环境中执行完整检查；
- Branch Protection：要求 CI 通过后才允许 Merge。

当前 Hook 每次都运行全部后端测试。测试规模增大后，可以把 Hook 改成快速单元测试，把完整 Integration Test 留给 CI；不要为了速度直接删除服务器端验证。

## 验证

先单独运行两条命令：

```bash
npm --prefix frontend run lint:staged
dotnet test tests/BlogApi.Tests/BlogApi.Tests.csproj
```

然后暂存一个可安全修改的文件并创建测试 Commit。若测试失败，应看到 Commit 被中止；修复后重新暂存被格式化的文件，再提交。

## 常见问题

- `husky: command not found`：先在 `frontend` 安装依赖。
- Hook 没有运行：检查 `core.hooksPath`，并确认不是用 `--no-verify` 提交。
- 找不到测试项目：确认 Hook 从 Git 根目录运行，路径不要写成相对 `frontend` 的形式。
- CI 安装依赖时触发 Husky：在 CI 的 `npm ci` 步骤设置 `HUSKY=0`。

## 参考资料

- [Husky documentation](https://typicode.github.io/husky/)
- [lint-staged documentation](https://github.com/lint-staged/lint-staged)
- [Git hooks](https://git-scm.com/docs/githooks)

## 主线导航

- 上一步：[使用 GitHub Actions 持续验证 .NET 与 Vue](./22-github-actions-ci.md)
- 下一步：[使用 GitHub OIDC 和 SSM 手动部署 EC2](./24-github-oidc-ssm-ec2-deploy.md)
