# 从零构建 ASP.NET Core 博客 API

这套笔记既是技术文章集合，也是一条可执行的项目主线。按“必做主线”顺序操作，可以从空目录逐步构建一个具备 PostgreSQL、EF Core Migration、CRUD、Validation、Slug、分页、Docker Compose、Service Layer、统一异常处理、Identity 管理员、JWT 角色授权、草稿发布、自动化测试、健康检查、结构化日志、可切换图片存储、CI、手动部署和集中日志的全栈博客。

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
| 22 | [使用 EF Core 为文章加入多分类](./14a-model-blog-categories-many-to-many.md) | Category、Many-to-many、Join Table | Category 和 BlogPost 多对多关系可查询 |
| 23 | [自动创建分类并按分类筛选文章](./14b-create-and-filter-blog-categories.md) | CategoryService、分类列表、Database Filter | 新分类自动建立，分页统计基于筛选结果 |
| 24 | [建立本地图片上传 API](./14c-build-local-image-upload-api.md) | Upload Controller、Image Storage Service、Static Files | 上传返回 URL，重建 Container 后文件仍存在 |
| 25 | [加入 Identity 管理员](./15-add-aspnet-core-identity-admin.md) | ApplicationUser、Identity Tables、AdminSeeder | 管理员和 Admin 角色只初始化一次 |
| 26 | [登录并签发 JWT](./15a-issue-jwt-access-token.md) | Login DTO、AuthController、JwtTokenService | 正确账号返回 Access Token，错误账号返回 401 |
| 27 | [使用 JWT Bearer 保护写接口](./15b-protect-blog-write-endpoints-jwt.md) | Bearer Validation、Authentication Middleware、Role Authorization | 游客可读，只有 Admin 可以写入和上传 |
| 28 | [为文章加入草稿、发布与取消发布](./16-add-draft-publish-workflow.md) | Publish Status、Published Time、Public Query Filter | 草稿对游客不可见，发布后可见，取消发布后再次隐藏 |
| 29 | [使用 xUnit 测试博客 API 工作流](./17-test-blog-api-workflows.md) | Unit Tests、WebApplicationFactory、Isolated Test Database | 授权与发布流程可重复验证 |
| 30 | [为 API 和数据库加入健康检查](./18-add-api-database-health-check.md) | `/health`、DbContext Check、Compose Healthcheck | API 与数据库就绪状态可被 Docker 判断 |
| 31 | [使用 Request Scope 建立结构化日志](./19-structured-logging-request-scope.md) | TraceId、UserId、Action Logs | 一次请求的日志可以被关联和检索 |
| 32 | [运行生产式 Docker Compose 栈](./20-production-compose-nginx.md) | Vue Build、Nginx、Production Compose | 只公开前端入口，三个服务健康运行 |
| 33 | [切换本地与 S3 图片存储](./21-switch-local-s3-image-storage.md) | Storage Provider、S3 Service、Presigned Read URL | Local 与 S3 两种模式分别上传并读取成功 |
| 34 | [使用 GitHub Actions 验证前后端](./22-github-actions-ci.md) | Backend/Frontend CI Jobs | Push 和 PR 显示两组独立检查 |
| 35 | [使用 Husky 检查前后端](./23-husky-pre-commit-fullstack.md) | Pre-commit、lint-staged、xUnit | 检查失败时 Commit 被阻止 |
| 36 | [使用 OIDC 和 SSM 手动部署 EC2](./24-github-oidc-ssm-ec2-deploy.md) | Deploy Role、OIDC、Run Command | 手动 Workflow 返回 SSM Success |
| 37 | [发送容器日志到 CloudWatch](./25-docker-cloudwatch-logs.md) | awslogs、Log Groups、EC2 Role | 三个服务的 stdout 可集中检索 |
| 38 | [保留 AWS 并部署私人 VPS](./26-vps-deployment-profile.md) | VPS Compose、Local Storage、SSH Workflow | VPS 健康运行且 Merge 不自动部署 |
| 39 | [使用 PostgreSQL 数组保存标签](./27-postgresql-array-tags.md) | `text[]` Migration、Tag Filter、Tag Folder | 标签与未标签筛选结果正确 |

## 概念补充

这些文章用于理解主线中的代码，不要求严格按顺序执行：

- [读懂 csproj](./00a-understand-dotnet-csproj.md)
- [读懂 Program.cs](./00b-understand-aspnet-core-program-cs.md)
- [Controller Attribute Routing](./00c-aspnet-core-controller-routing.md)
- [本地启动与排错](./00d-run-and-test-aspnet-core-api.md)
- [读懂 Dockerfile 与 Docker Compose 的分工](./03b-understand-dockerfile-and-compose.md)
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

### 分类与图片上传完成时

```text
BlogPost ← BlogPostCategories → Category
CategoryService → 复用已有分类并创建缺失分类
GET /api/blogs?categorySlug=... → 在数据库中筛选后分页
POST /api/images → 单张图片写入 wwwroot/uploads
UseStaticFiles → 通过 /uploads/... 返回图片
Docker Bind Mount → 重建 Container 后保留上传文件
```

### Identity 与 JWT 授权完成时

```text
ApplicationUser + IdentityDbContext → 保存用户、密码哈希和角色
AdminSeeder → 启动时初始化管理员和 Admin 角色
POST /api/auth/login → 验证密码并签发 JWT
UseAuthentication → 验证 Bearer Token 并建立 HttpContext.User
UseAuthorization → 检查 [Authorize(Roles = "Admin")]
GET → 游客可访问
POST、PUT、DELETE、图片上传 → 只有 Admin 可访问
```

### 发布、测试与可观测性完成时

```text
BlogPost.Status + PublishedAtUtc → 区分 Draft 与 Published
Publish / Unpublish → 显式改变文章发布状态
xUnit + WebApplicationFactory → 验证授权和发布工作流
GET /health → 同时检查 API 与 AppDbContext
Request Scope → 为同一次请求附加 TraceId 与 UserId
Structured Logs → 记录创建、更新、发布和删除动作
```

### 生产化与双部署配置完成时

```text
Production Compose → Nginx + ASP.NET Core + PostgreSQL
Storage Provider   → Local Volume 或 Amazon S3
GitHub Actions CI  → 独立验证 Backend 与 Frontend
Manual Deploy      → AWS OIDC + SSM 或 VPS SSH
CloudWatch Logs    → 收集 AWS Profile 的 Container stdout/stderr
PostgreSQL text[]  → 保存并筛选轻量文章 Tags
```

## 最终目录结构

```text
BlogApi/
├── Controllers/
│   ├── AuthController.cs
│   ├── BlogsController.cs
│   ├── CategoriesController.cs
│   └── UploadsController.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── Seeding/
│   │   └── AdminSeeder.cs
│   └── Migrations/
├── Dtos/
│   ├── BlogPostDto.cs
│   ├── BlogPostResponseDto.cs
│   ├── CategoryResponseDto.cs
│   ├── LoginDto.cs
│   ├── LoginResponseDto.cs
│   ├── PaginationQueryDto.cs
│   ├── PagedResultDto.cs
│   └── UploadImageRequestDto.cs
├── Models/
│   ├── ApplicationUser.cs
│   ├── BlogPost.cs
│   └── Category.cs
├── Options/
│   ├── JwtOptions.cs
│   └── S3StorageOptions.cs
├── Services/
│   ├── Auth/
│   │   ├── JwtTokenResult.cs
│   │   └── JwtTokenService.cs
│   ├── ImageStorage/
│   │   ├── IImageStorageService.cs
│   │   ├── ImageUploadResult.cs
│   │   ├── LocalImageStorageService.cs
│   │   └── S3ImageStorageService.cs
│   ├── BlogPostService.cs
│   └── CategoryService.cs
├── Exceptions/
│   └── SlugConflictExceptionHandler.cs
├── Middleware/
│   └── RequestLogContextMiddleware.cs
├── tests/
│   └── BlogApi.Tests/
│       ├── BlogApiFactory.cs
│       ├── BlogAuthorizationTests.cs
│       ├── BlogPostDomainTests.cs
│       ├── BlogPostDtoValidationTests.cs
│       └── HealthCheckTests.cs
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
├── compose.production.yaml
├── compose.vps.yaml
├── Makefile
├── .env.example
├── .github/workflows/
│   ├── ci.yml
│   ├── deploy.yml
│   └── deploy-vps.yml
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
