---
title: "用 Bash 安全执行 EF Core Database Update 与回滚"
description: "封装开发环境 Connection String、目标 Migration 验证和 database update，并通过 Makefile 提供升级与回滚入口。"
tags:
  - Bash
  - EF Core
  - Migration
  - Database Update
---

# 用 Bash 安全执行 EF Core Database Update 与回滚

生成 Migration 和应用 Migration 是两个不同动作。前者创建代码文件，后者真正修改 PostgreSQL Schema。

本文把 `database update` 封装成可重复执行的项目脚本，并允许显式指定目标版本。

## 创建更新脚本

新建 `scripts/update-database.sh`：

```bash
#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$PROJECT_ROOT/.env.development"

if [[ $# -gt 1 ]]; then
    echo "Usage: ./scripts/update-database.sh [TargetMigration]" >&2
    exit 64
fi

TARGET_MIGRATION="${1:-}"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing environment file: $ENV_FILE" >&2
    exit 1
fi

cd "$PROJECT_ROOT"

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

: "${POSTGRES_DB:?POSTGRES_DB is missing}"
: "${POSTGRES_USER:?POSTGRES_USER is missing}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is missing}"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

if ! dotnet ef --version >/dev/null 2>&1; then
    echo "Run 'dotnet tool restore' first." >&2
    exit 1
fi

if [[ -n "$TARGET_MIGRATION" ]]; then
    dotnet ef database update "$TARGET_MIGRATION" \
        --context AppDbContext
else
    dotnet ef database update \
        --context AppDbContext
fi
```

增加执行权限：

```bash
chmod +x scripts/update-database.sh
```

## 更新到最新版本

```bash
./scripts/update-database.sh
```

不传目标时，EF Core 应用所有尚未执行的 Migration。

## 更新或回退到指定版本

```bash
./scripts/update-database.sh InitialCreate
```

如果目标早于当前版本，EF Core 会执行后续 Migration 的 `Down()`。

回退全部 Migration：

```bash
./scripts/update-database.sh 0
```

回退可能删除 Table、Column 和数据。在有价值的数据环境中，执行前必须检查 `Down()` 和备份策略。

## 加入 Makefile

```makefile
.PHONY: db-update db-rollback

db-update:
	./scripts/update-database.sh

db-rollback:
	@test -n "$(TARGET)" || \
		(echo "Usage: make db-rollback TARGET=InitialCreate"; exit 1)
	./scripts/update-database.sh "$(TARGET)"
```

使用：

```bash
make db-update
make db-rollback TARGET=InitialCreate
```

Makefile 提供短入口，Shell Script 负责读取环境、检查依赖和执行 EF Core Command。

## 为什么不让 API 启动时自动 Migration

单实例本地开发中可以在启动时调用 Migration，但 Production 多实例部署可能出现多个进程同时修改 Schema。

更稳妥的方式是：

```text
生成并 Review Migration
        ↓
由单独 Script、CI/CD Step 或 Migration Job 应用
        ↓
再启动或滚动部署 API
```

## 验证

```sql
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
```

确认目标 Migration 已记录，再查询本次变更涉及的 Table 或 Column。

## 完成标准

- Script 可以在项目根目录外调用；
- 没有把 Connection String 打印到日志；
- `make db-update` 可以应用最新 Migration；
- 指定目标前必须显式提供参数；
- 回退前会检查 `Down()` 和数据风险。

## 参考资料

- [EF Core database update](https://learn.microsoft.com/ef/core/cli/dotnet#dotnet-ef-database-update)
- [Applying migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)

## 主线导航

- 上一步：[封装 Migration 生成脚本](./05-safe-bash-automation-for-dotnet.md)
- 下一步：[实现 REST CRUD](./07-aspnet-core-ef-core-crud-api.md)
