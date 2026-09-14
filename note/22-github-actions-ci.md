---
title: "使用 GitHub Actions 持续验证 .NET 与 Vue"
description: "将后端 Build/Test 和前端 Lint/Type Check/Test/Build 放入独立 CI Job，阻止未经验证的变更进入主分支。"
tags:
  - GitHub Actions
  - CI
  - xUnit
  - Vue
---

# 使用 GitHub Actions 持续验证 .NET 与 Vue

本地测试是否执行取决于开发者操作。CI 的作用是在统一、干净的 Runner 上重新验证仓库，给 Pull Request 和主分支一个可重复的结果。

## CI 与部署不是一件事

```text
CI：代码是否能通过检查？
CD：把哪一个已验证版本部署到哪里？
```

本项目把它们放在不同 Workflow。CI 在 Push 和 Pull Request 时自动运行；部署使用手动 `workflow_dispatch`，不会因为 Merge 就自动改动服务器。

## 触发条件与最小权限

`.github/workflows/ci.yml`：

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

CI 只需要读取代码，因此不授予写权限。`concurrency` 会取消同一分支已经过时的检查，避免连续 Push 浪费 Runner 时间。

## 后端 Job

```yaml
backend:
  runs-on: ubuntu-latest
  timeout-minutes: 15
  steps:
    - uses: actions/checkout@v6

    - uses: actions/setup-dotnet@v5
      with:
        dotnet-version: 10.0.x

    - name: Restore
      run: dotnet restore tests/BlogApi.Tests/BlogApi.Tests.csproj

    - name: Build
      run: >
        dotnet build tests/BlogApi.Tests/BlogApi.Tests.csproj
        --configuration Release --no-restore

    - name: Test
      run: >
        dotnet test tests/BlogApi.Tests/BlogApi.Tests.csproj
        --configuration Release --no-build --verbosity normal
```

Restore、Build、Test 分开后，失败阶段更容易定位。`--no-restore` 和 `--no-build` 保证后续步骤使用前一步的产物，而不是偷偷重复工作。

测试 Factory 使用隔离配置和测试数据库，因此 CI 不需要连接生产 PostgreSQL，也不应读取生产 Secret。

## 前端 Job

```yaml
frontend:
  runs-on: ubuntu-latest
  timeout-minutes: 15
  defaults:
    run:
      working-directory: frontend
  steps:
    - uses: actions/checkout@v6

    - uses: actions/setup-node@v7
      with:
        node-version: 24
        cache: npm
        cache-dependency-path: frontend/package-lock.json

    - name: Install dependencies
      env:
        HUSKY: 0
      run: npm ci

    - run: npx --no-install oxlint .
    - run: npx --no-install eslint . --no-cache
    - run: npm run type-check
    - run: npm run test:unit -- --run
    - run: npm run build-only
```

`npm ci` 严格使用 Lockfile，适合 CI。`HUSKY=0` 避免在临时 Runner 中安装本地 Git Hook。`--no-install` 防止命令缺失时临时下载未知版本。

Lint、类型检查、单元测试和 Production Build 解决不同问题：

- Lint：可疑语法和规则违规；
- Type Check：TypeScript/Vue 类型错误；
- Unit Test：组件和状态逻辑行为；
- Build：Vite 生产打包是否成立。

任意一步失败，Job 就失败。

## 为什么前后端使用两个 Job

两个 Job 可以并行执行，并在 GitHub UI 中分别显示 Backend/Frontend 状态。前端失败不会隐藏后端结果，反之亦然。代价是两个 Runner 都需要 Checkout，但对小型项目而言可读性更重要。

## 验证

提交 Workflow 后，打开 GitHub 仓库的 Actions 页面。一次成功运行应看到两个绿色 Job：

```text
Backend  → Restore → Build → Test
Frontend → npm ci → Lint → Type Check → Unit Test → Build
```

不要只验证 Workflow YAML 被接受。可以在测试分支故意制造一个断言失败，确认 PR Check 变红，然后撤销该测试变更。

## CI 不能替代什么

- 不能证明生产服务器配置正确；
- 不能替代 PostgreSQL 专属集成测试，因为当前测试数据库不是 PostgreSQL；
- 不能替代浏览器端到端测试，除非把 Playwright Job 明确加入；
- 不能自动保护主分支，仍需在 GitHub Branch Protection 中把两个 Job 设为 Required Checks。

## 参考资料

- [Building and testing .NET](https://docs.github.com/actions/automating-builds-and-tests/building-and-testing-net)
- [Building and testing Node.js](https://docs.github.com/actions/automating-builds-and-tests/building-and-testing-nodejs)
- [Workflow permissions](https://docs.github.com/actions/using-jobs/assigning-permissions-to-jobs)

## 主线导航

- 上一步：[通过配置切换本地与 S3 图片存储](./21-switch-local-s3-image-storage.md)
- 下一步：[使用 Husky 在提交前检查前后端](./23-husky-pre-commit-fullstack.md)
