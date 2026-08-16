# 从零构建 ASP.NET Core 博客 API

这套笔记既是技术文章集合，也是一条可执行的项目主线。按“必做主线”顺序操作，可以从空目录逐步构建一个具备 PostgreSQL、EF Core Migration、CRUD、Validation、Slug、分页、Docker Compose、Service Layer 和统一异常处理的博客 API。

文章统一使用：

```text
Project Name: BlogApi
Target Framework: .NET 10
Database: PostgreSQL
API Base Route: /api/blogs
```

## 开始前准备

开发机需要：

- .NET 10 SDK；
- Docker Desktop 或 Docker Engine + Compose；
- Git；
- 一个 HTTP Client，例如 `.http` 文件、Postman 或 curl；
- 可选的 VS Code PostgreSQL Extension。

每完成一步都先执行该文章的验证，不要在 Build 或 Runtime Error 尚未解决时继续下一篇。

## 必做主线

| 步骤 | 文章 | 主要产物 | 完成验证 |
|---:|---|---|---|
| 1 | [创建并运行 Controller API](./00-create-aspnet-core-controller-web-api.md) | `BlogApi.csproj`、`Program.cs`、第一个 Controller | `GET /api/blogs` 返回 200 |
| 2 | [理解数据库组件职责](./01-aspnet-core-postgresql-ef-core-setup.md) | 明确技术边界，不修改代码 | 能解释 EF Core、Npgsql、Docker 和 Migration |
| 3 | [安装 EF Core PostgreSQL 工具链](./01a-install-ef-core-postgresql-tools.md) | PackageReference、Local Tool Manifest | `dotnet-ef --version` 成功 |
| 4 | [启动 PostgreSQL 开发数据库](./02-docker-compose-postgresql-development.md) | `.env.development`、`compose.yaml`、Named Volume | `pg_isready` 与 SQL 查询成功 |
| 5 | [配置 EF Core DbContext](./04-aspnet-core-ef-core-postgresql-dbcontext-migrations.md) | Entity、DbContext、Connection String、DI Registration | API 能执行数据库 Query |
| 6 | [创建第一份 Migration](./04b-create-first-ef-core-migration.md) | `InitialCreate`、Model Snapshot、Database Table | History 与 `BlogPosts` Table 可查询 |
| 7 | [封装 Migration 生成脚本](./05-safe-bash-automation-for-dotnet.md) | `add-migration.sh` 与 Makefile Command | `make migration NAME=...` 可用 |
| 8 | [封装 Database Update 与回滚](./05a-safe-ef-core-database-update-script.md) | `update-database.sh` | `make db-update` 可用 |
| 9 | [实现 REST CRUD](./07-aspnet-core-ef-core-crud-api.md) | GET、POST、PUT、DELETE | 完整 CRUD HTTP 流程通过 |
| 10 | [加入 Request Validation](./08-aspnet-core-dto-common-attributes.md) | DataAnnotations DTO | 无效 Body 自动返回 400 |
| 11 | [构建 API Docker Image](./03-containerize-aspnet-core-api-with-docker.md) | Multi-stage Dockerfile | Runtime Image 启动且不含源码 |
| 12 | [把 API 加入 Compose](./03a-add-aspnet-core-api-to-compose.md) | API + PostgreSQL Compose Services | Container API 能查询 PostgreSQL |
| 13 | [配置 Container 热更新](./06-aspnet-core-docker-compose-hot-reload.md) | Development Stage、Compose Watch、`make dev` | 修改方法后自动更新或重启 |
| 14 | [为已有表安全添加 Slug](./09-ef-core-add-required-column-with-existing-data.md) | 三阶段 Migration、NOT NULL、Unique Index | 旧数据保留且全部有 Slug |
| 15 | [把 Slug 接入文章 API](./09a-add-slug-to-blog-api.md) | Slug DTO、Mapping 与按 Slug 查询 | 创建和查询 Slug 成功 |
| 16 | [把唯一冲突转换成 409](./10-aspnet-core-postgresql-unique-conflict-409.md) | Npgsql Error Mapping、ProblemDetails | 重复 Slug 返回 409 |
| 17 | [实现稳定分页](./11-ef-core-stable-pagination.md) | Query DTO、`PagedResult<T>`、Skip/Take | 分页与非法参数测试通过 |
| 18 | [把分页查询移入 Service](./12a-refactor-pagination-to-service.md) | Scoped `BlogPostService` | 重构前后 Response 一致 |
| 19 | [把完整 CRUD 迁入 Service](./12b-refactor-crud-to-service.md) | Service CRUD、精简后的 Controller | CRUD 行为不变，Controller 不再依赖 DbContext |
| 20 | [使用 Response DTO 隔离 EF Core Entity](./12c-use-response-dto-in-service.md) | Response DTO、查询投影、Entity/DTO Mapping | GET、POST、PUT 不再直接返回 Entity |
| 21 | [使用 IExceptionHandler 统一处理 Slug 冲突](./13-aspnet-core-iexceptionhandler-postgresql-conflict.md) | 具体异常 Handler、ProblemDetails、异常处理中间件 | 重复 Slug 由全局 Pipeline 返回 409 |

## 概念补充

这些文章用于理解主线中的代码，不要求严格按顺序执行：

- [读懂 csproj](./00a-understand-dotnet-csproj.md)
- [读懂 Program.cs](./00b-understand-aspnet-core-program-cs.md)
- [Controller Attribute Routing](./00c-aspnet-core-controller-routing.md)
- [本地启动与排错](./00d-run-and-test-aspnet-core-api.md)
- [OnModelCreating Hook](./04a-ef-core-onmodelcreating-hook.md)
- [System.Text.Json Attribute](./08a-system-text-json-dto-attributes.md)
- [Body、Route、Query 与 Header Binding](./08b-aspnet-core-model-binding-sources.md)
- [ActionFilter 的适用场景与职责边界](./08c-aspnet-core-action-filter-boundaries.md)
- [什么时候需要 Service Layer](./12-when-to-add-service-layer.md)
- [读懂 IExceptionHandler 方法契约](./13a-understand-iexceptionhandler-contract.md)
- [PostgreSQL Sequence 为什么产生 ID 缺口](./14-postgresql-sequence-id-gaps.md)

## 主线中的状态演进

### CRUD 完成时

```text
BlogPost: Id, Title, Content, CreatedAtUtc
API: GET, POST, PUT, DELETE
Database: BlogPosts
```

### Slug 完成时

```text
BlogPost 增加 Slug
Database 增加 NOT NULL + Unique Index
API 支持 GET /api/blogs/by-slug/{slug}
重复 Slug 返回 409
```

### 分页与 Service 完成时

```text
GET /api/blogs?page=1&pageSize=10
Controller 负责 HTTP Binding 与 Response
BlogPostService 负责分页查询和完整 CRUD
AppDbContext 负责 EF Core 数据访问
GET、POST、PUT 使用 BlogPostResponseDto
DELETE 成功返回 204 No Content
```

### 全局异常处理完成时

```text
数据库约束负责阻止重复 Slug
具体 IExceptionHandler 负责把已知冲突转换成 409
Controller 不再重复捕获相同 DbUpdateException
未知异常继续进入后续 Handler 或后备错误处理
```

## 最终目录结构

```text
BlogApi/
├── Controllers/
│   └── BlogsController.cs
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Dtos/
│   ├── BlogPostDto.cs
│   ├── BlogPostResponseDto.cs
│   ├── PaginationQueryDto.cs
│   └── PagedResultDto.cs
├── Models/
│   └── BlogPost.cs
├── Services/
│   └── BlogPostService.cs
├── Exceptions/
│   └── SlugConflictExceptionHandler.cs
├── scripts/
│   ├── add-migration.sh
│   └── update-database.sh
├── .config/
│   └── dotnet-tools.json
├── Properties/
│   └── launchSettings.json
├── Dockerfile
├── .dockerignore
├── compose.yaml
├── Makefile
├── .env.example
├── appsettings.json
├── appsettings.Development.json
├── BlogApi.http
├── BlogApi.csproj
└── Program.cs
```

## 每个阶段的固定验证

```bash
dotnet build
docker compose --env-file .env.development config --quiet
```

功能发生变化时，再执行对应 HTTP Test。不要只看 Status Code；创建、更新和删除后应重新 GET，确认数据库中的真实状态。

## 阅读方式

- 想从零重做项目：严格按照“必做主线”。
- 已经有项目、只遇到一个问题：按标题打开对应文章。
- 只想理解语法：阅读“概念补充”。
- 主线文章假设上一篇已完成；概念文章不会偷偷改变项目状态。
