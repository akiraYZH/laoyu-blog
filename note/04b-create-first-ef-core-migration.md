---
title: "创建并应用第一份 EF Core Migration"
description: "理解 Model Snapshot、Up、Down、Migration History，以及生成、检查和应用 InitialCreate 的安全流程。"
tags:
  - EF Core
  - Migration
  - PostgreSQL
  - dotnet-ef
---

# 创建并应用第一份 EF Core Migration

Entity 与 DbContext 配置完成后，PostgreSQL 中仍然不会自动出现 Table。Migration 负责把当前 EF Core Model 转换为可检查、可提交和可重复应用的 Schema Change。

## 准备 Design-time Connection String

`dotnet-ef` 在开发机运行，因此需要使用 PostgreSQL 发布到开发机的端口：

```bash
set -a
source .env.development
set +a

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
```

确认 PostgreSQL Service 已经启动并健康：

```bash
docker compose \
  --env-file .env.development \
  up --detach postgres
```

后续命令应在同一个 Terminal Session 中执行。下一篇会把这些重复步骤封装成 Script。

## 生成 InitialCreate

```bash
dotnet tool run dotnet-ef migrations add InitialCreate \
  --context AppDbContext \
  --output-dir Data/Migrations
```

参数职责：

```text
migrations add         → 生成新的 Migration
InitialCreate          → Migration Name
--context              → 使用哪个 DbContext
--output-dir           → 文件生成位置
```

这条命令只生成文件，不会修改 PostgreSQL。

## 生成的文件

```text
Data/Migrations/
├── 时间戳_InitialCreate.cs
├── 时间戳_InitialCreate.Designer.cs
└── AppDbContextModelSnapshot.cs
```

### Migration 主文件

省略 Provider-specific Metadata 后，核心结构类似：

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "BlogPosts",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false),
            Title = table.Column<string>(nullable: false),
            Content = table.Column<string>(nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_BlogPosts", post => post.Id);
        });
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "BlogPosts");
}
```

`Up()` 描述升级数据库要做什么；`Down()` 描述撤销这一版本要做什么。

实际由 Npgsql 生成的文件还可能包含 Identity Strategy 等 Annotation，应保留并检查工具生成的完整内容，不要用这个简化片段覆盖生成文件。

### Designer 文件

Designer 保存这份 Migration 对应的 Model Metadata，通常由工具维护，不手工编辑。

### Model Snapshot

Snapshot 表示最新 EF Core Model。下次执行 `migrations add` 时，EF Core 比较当前 Model 与 Snapshot，生成差异。

## 先检查再应用

检查 `Up()`：

- 是否只创建预期 Table；
- Column Type 和 Nullable 是否正确；
- Primary Key、Foreign Key 和 Index 是否正确；
- 是否存在意外 Drop 或 Rename。

不要因为文件由工具生成就跳过 Review。

## 应用 Migration

```bash
dotnet tool run dotnet-ef database update \
  --context AppDbContext
```

EF Core 会：

```text
读取已应用版本
        ↓
获取 Migration Lock
        ↓
执行尚未应用的 Up()
        ↓
写入 __EFMigrationsHistory
```

## `__EFMigrationsHistory`

这个 Table 记录数据库已经应用的 Migration：

```sql
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
```

它让 EF Core 知道哪些版本已经执行，避免每次 Update 都重新创建 Table。

## 更新到指定版本

```bash
dotnet tool run dotnet-ef database update MigrationName
```

指定较早版本时，EF Core 会按照 `Down()` 回退后续 Migration。回退 Schema 可能丢失 Column 或数据，执行前必须检查目标和备份策略。

## 为什么企业使用 Migration

手工执行 SQL 无法可靠回答每个环境已经执行过哪些变更。Migration 可以进入 Pull Request，接受 Review，并由 CI/CD 或一次性 Job 应用。

Production 不应让多个 API Instance 在启动时同时修改 Schema。更常见的方式是独立 Migration Step 或 Migration Bundle。

## 验证

除了看到 `Done.`，还应验证：

```sql
SELECT * FROM "__EFMigrationsHistory";
SELECT * FROM "BlogPosts" LIMIT 1;
```

第一条证明版本已记录，第二条证明目标 Table 可查询。

## 总结

```text
Model       → 应用期望的数据库结构
Migration   → 从旧结构到新结构的变更
Snapshot    → 最新 Model 基线
History     → 数据库已应用的版本
database update → 真正执行 Schema Change
```

## 参考资料

- [EF Core migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Applying migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)

## 主线导航

- 上一步：[配置 EF Core DbContext](./04-aspnet-core-ef-core-postgresql-dbcontext-migrations.md)
- 下一步：[封装 Migration Script](./05-safe-bash-automation-for-dotnet.md)
