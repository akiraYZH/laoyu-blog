---
title: "从重复命令到项目脚本：用 Bash 封装 EF Core Migration"
description: "把环境变量和 dotnet-ef 命令封装成可重复使用的 Bash Script，并理解参数、严格模式、路径定位和 Secret 处理。"
tags:
  - Bash
  - Shell Script
  - .NET
  - EF Core
  - Automation
---

# 从重复命令到项目脚本：用 Bash 封装 EF Core Migration

创建 EF Core Migration 时，需要先读取开发环境变量，再为开发机准备 Connection String：

```bash
source .env.development

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"

dotnet ef migrations add InitialCreate \
  --context AppDbContext \
  --output-dir Data/Migrations
```

第一次手动执行有助于理解流程，但不应该长期要求开发者记住这些细节。我们把它封装成：

```bash
./scripts/add-migration.sh InitialCreate
```

## 从最小脚本开始

最简单的版本只有几行：

```bash
#!/usr/bin/env bash

source .env.development

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"

dotnet ef migrations add "$1" \
  --context AppDbContext \
  --output-dir Data/Migrations
```

这里最重要的语法是：

| 语法 | 含义 |
|---|---|
| `#!/usr/bin/env bash` | 使用 Bash 执行脚本 |
| `source file` | 在脚本中读取环境文件 |
| `$1` | 用户传入的第一个参数 |
| `$VARIABLE` | 读取变量 |
| `export` | 把变量传给子进程 |
| `\` | 命令在下一行继续 |

执行：

```bash
./scripts/add-migration.sh InitialCreate
```

脚本中的 `$1` 就是 `InitialCreate`。

这个版本能够工作，但缺少错误检查：忘记参数、环境文件不存在或 Tool 没有恢复时，错误信息都不够清楚。

## 加入必要的安全检查

项目最终使用下面的脚本：

```bash
#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$PROJECT_ROOT/.env.development"

if [[ $# -ne 1 ]]; then
    echo "Usage: ./scripts/add-migration.sh <MigrationName>" >&2
    echo "Example: ./scripts/add-migration.sh InitialCreate" >&2
    exit 64
fi

MIGRATION_NAME="$1"

if [[ ! "$MIGRATION_NAME" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
    echo "Migration name must be a valid C# identifier." >&2
    echo "Example: InitialCreate or AddPublishedAt" >&2
    exit 64
fi

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing development environment file: $ENV_FILE" >&2
    exit 1
fi

cd "$PROJECT_ROOT"

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

: "${POSTGRES_DB:?POSTGRES_DB is missing from .env.development}"
: "${POSTGRES_USER:?POSTGRES_USER is missing from .env.development}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is missing from .env.development}"

export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

if ! dotnet ef --version >/dev/null 2>&1; then
    echo "The project-local dotnet-ef tool is not available." >&2
    echo "Run 'dotnet tool restore' and try again." >&2
    exit 1
fi

echo "Creating migration '$MIGRATION_NAME' for AppDbContext..."

dotnet ef migrations add "$MIGRATION_NAME" \
    --context AppDbContext \
    --output-dir Data/Migrations
```

完整脚本比最小版本长，但新增内容主要解决四类问题。

## 1. 出错时立即停止

```bash
set -euo pipefail
```

- `-e`：命令失败后停止；
- `-u`：使用未定义变量时报错；
- `pipefail`：Pipeline 中任一步失败都算失败。

这样可以避免 Migration 失败后，脚本仍继续执行并给出误导结果。

## 2. 不依赖当前目录

```bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
```

它先找到脚本自己的目录，再取得项目根目录。无论从哪里调用脚本，都能找到：

```text
.env.development
.config/dotnet-tools.json
Data/Migrations
```

这段写法可以当作路径定位模板使用，不需要每次从头推导。

## 3. 检查输入

```bash
if [[ $# -ne 1 ]]; then
```

要求用户只提供一个 Migration Name。

```bash
if [[ ! "$MIGRATION_NAME" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
```

把名称限制为适合 C# Class 的简单格式：

```text
InitialCreate       正确
AddPublishedAt      正确
123Migration        错误
Add-Published-At    错误
```

脚本还会提前检查 `.env.development`、数据库变量和项目级 `dotnet-ef`，让错误发生在最接近原因的位置。

## 4. 不输出 Secret

```bash
export ConnectionStrings__DefaultConnection="..."
```

只把 Connection String 传给 `dotnet ef`，脚本不会打印它。

执行脚本时：

```bash
./scripts/add-migration.sh InitialCreate
```

Shell 会创建子进程。脚本结束后，其中 Export 的 Connection String 不会残留到当前 Terminal。

不要这样调用：

```bash
source scripts/add-migration.sh InitialCreate
```

`source` 会让脚本直接在当前 Shell 中运行，环境变量可能残留。

调试包含 Secret 的脚本时也不要随意开启：

```bash
set -x
```

因为它可能打印变量展开后的完整命令。

## 使用脚本

第一次需要增加执行权限：

```bash
chmod +x scripts/add-migration.sh
```

团队成员 Clone 项目后恢复 Local Tool：

```bash
dotnet tool restore
```

创建第一个 Migration：

```bash
./scripts/add-migration.sh InitialCreate
```

以后模型发生变化时，只修改名称：

```bash
./scripts/add-migration.sh AddPublishedAt
```

成功时通常看到：

```text
Creating migration 'InitialCreate' for AppDbContext...
Build started...
Build succeeded.
Done.
```

脚本只生成 Migration Files，不执行 `database update`。生成后仍然要检查 `Up()` 和 `Down()`。

## 常见错误

没有执行权限：

```text
permission denied: ./scripts/add-migration.sh
```

解决：

```bash
chmod +x scripts/add-migration.sh
```

Local Tool 尚未恢复：

```text
The project-local dotnet-ef tool is not available.
```

解决：

```bash
dotnet tool restore
```

只检查 Bash Syntax，不真正运行脚本：

```bash
bash -n scripts/add-migration.sh
```

## Makefile 负责统一入口

当项目出现更多常用命令时，可以增加 Makefile：

```makefile
.PHONY: build up down logs migration

build:
	dotnet build

up:
	docker compose --env-file .env.development up --detach --build

down:
	docker compose --env-file .env.development down

logs:
	docker compose --env-file .env.development logs --follow api

migration:
	@test -n "$(NAME)" || \
		(echo "Usage: make migration NAME=InitialCreate"; exit 1)
	./scripts/add-migration.sh "$(NAME)"
```

开发者使用：

```bash
make migration NAME=InitialCreate
```

Makefile 负责提供简短命令，Shell Script 继续负责环境变量、检查和具体实现。不要把同一套 Connection String 逻辑复制到两个地方。

## 小结

Shell Script 不过是保存到文件中的 Terminal Commands。`add-migration.sh` 的核心只有三步：

```text
读取 Development 环境
        ↓
准备宿主机 Connection String
        ↓
执行 dotnet ef
```

其余代码用于提前发现错误，并避免 Secret 出现在日志中。

不需要从记忆中写出所有 Bash 模板。实际开发中更重要的是能看懂主流程、知道如何使用，并在修改公共脚本时通过测试和 Code Review 保证安全。

## 参考资料

- [Bash Reference Manual](https://www.gnu.org/software/bash/manual/bash.html)
- [ShellCheck](https://www.shellcheck.net/)
- [EF Core CLI](https://learn.microsoft.com/ef/core/cli/dotnet)
- [GNU Make Manual](https://www.gnu.org/software/make/manual/)

## 主线导航

- 上一步：[创建第一份 EF Core Migration](./04b-create-first-ef-core-migration.md)
- 下一步：[封装 Database Update 与回滚](./05a-safe-ef-core-database-update-script.md)
