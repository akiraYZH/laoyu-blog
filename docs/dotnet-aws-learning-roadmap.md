# 前端开发者的 C#/.NET + AWS 学习与项目路线

> 目标：用一个可运行、可测试、可部署的博客后端，在 6～9 周内建立实用的 C#/.NET 全栈能力，并通过少量 AWS 功能获得真实云端经验。

## 1. 最终成果

完成后，项目应当具备以下能力：

- 使用 C# 和 ASP.NET Core Minimal API 提供 REST API。
- 使用 EF Core 和 PostgreSQL 持久化数据。
- 使用 JWT 完成注册、登录和权限保护。
- 使用统一验证和 `ProblemDetails` 返回错误。
- 使用 xUnit 编写单元测试和 API 集成测试。
- 使用 Docker Compose 在本地运行 API 和 PostgreSQL。
- 使用 Docker 将 API 和 PostgreSQL 部署到单台 EC2。
- 使用 S3 保存文章图片。
- 使用一个 S3 Event → Lambda Worker 学习原生 Lambda。
- 能解释项目架构、请求生命周期、数据库查询和部署过程。

当前阶段不要求掌握 ECS、ALB、RDS、Redis、Kubernetes 或微服务。

## 2. 时间与节奏

建议投入：

- 每周 5～7 小时。
- 每次 45～90 分钟。
- 预计 6～9 周完成第一版。
- 每周至少保留一次 2 小时的连续编码时间。

建议比例：

```text
20% 阅读文档和概念
70% 编写项目
10% 整理笔记和复盘
```

不要等“全部学会”再写项目。每学到一个概念，立即在博客 API 中使用。

## 3. 项目架构

第一版保持简单：

```text
前端
  ↓ HTTP/JSON
ASP.NET Core Minimal API
  ↓ EF Core
PostgreSQL
  ↓
Docker Compose / EC2

文章图片
  ↓
Amazon S3
  ↓ S3 Event
Lambda Worker
```

主 API 是长期运行的 ASP.NET Core 服务。Lambda 只负责一个小型事件任务，用来学习 AWS Serverless，不把整个 API 改成 Lambda。

## 4. 推荐项目结构

```text
laoyu-blog-backend/
├── LaoyuBlog.sln
├── src/
│   ├── LaoyuBlog.Api/
│   │   ├── Program.cs
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   ├── Posts/
│   │   │   └── Images/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   ├── Models/
│   │   ├── Middleware/
│   │   ├── Options/
│   │   ├── appsettings.json
│   │   └── LaoyuBlog.Api.csproj
│   └── LaoyuBlog.ImageWorker/
│       ├── Function.cs
│       └── LaoyuBlog.ImageWorker.csproj
├── tests/
│   ├── LaoyuBlog.UnitTests/
│   └── LaoyuBlog.IntegrationTests/
├── infra/
│   ├── docker/
│   │   └── docker-compose.yml
│   └── aws/
│       └── template.yaml
├── docs/
│   └── dotnet-aws-learning-roadmap.md
├── Dockerfile
├── .env.example
└── README.md
```

第一版不拆成 `Domain`、`Application`、`Infrastructure` 四个项目。等业务明显增长或出现第二个应用入口时再拆。

## 5. 功能范围

### 必须实现

```text
POST   /api/auth/register
POST   /api/auth/login

POST   /api/posts
GET    /api/posts
GET    /api/posts/{id}
GET    /api/posts/by-slug/{slug}
PUT    /api/posts/{id}
DELETE /api/posts/{id}
POST   /api/posts/{id}/publish

POST   /api/posts/{id}/image
GET    /health
```

### 暂时不实现

- 评论系统。
- 多租户。
- 实时通知。
- 全文搜索。
- 推荐算法。
- 微服务。
- 复杂角色系统。
- 管理后台。

## 6. 第 0 周：环境与项目初始化

### 学习内容

- 安装 .NET 10 SDK。
- 选择 VS Code、Visual Studio Community 或 Rider。
- 安装 Docker Desktop。
- 熟悉 `dotnet` CLI。
- 创建解决方案、API 项目和测试项目。

### 建议命令

```bash
dotnet --info
dotnet new sln -n LaoyuBlog
dotnet new webapi -n LaoyuBlog.Api -f net10.0 -o src/LaoyuBlog.Api
dotnet new xunit -n LaoyuBlog.UnitTests -f net10.0 -o tests/LaoyuBlog.UnitTests
dotnet new xunit -n LaoyuBlog.IntegrationTests -f net10.0 -o tests/LaoyuBlog.IntegrationTests
dotnet sln add src/LaoyuBlog.Api/LaoyuBlog.Api.csproj
dotnet sln add tests/LaoyuBlog.UnitTests/LaoyuBlog.UnitTests.csproj
dotnet sln add tests/LaoyuBlog.IntegrationTests/LaoyuBlog.IntegrationTests.csproj
dotnet build
dotnet test
```

### 完成标准

- [ ] `dotnet build` 成功。
- [ ] `dotnet test` 成功。
- [ ] 本地能够访问一个测试 API。
- [ ] 能解释 Solution 和 Project 的区别。

## 7. 第 1 周：从 TypeScript 迁移到 C#

### 必须学习

- `class`、`record`、`interface`。
- Value Type 与 Reference Type。
- Nullable Reference Types。
- Collection 和 Generic。
- LINQ。
- Exception。
- `Task`、`async/await`、`CancellationToken`。
- Dependency Injection 基础。

### TypeScript 对照

| TypeScript | C# |
|---|---|
| `interface` | `interface` |
| `type` / DTO | `record` 或 `class` |
| `Promise<T>` | `Task<T>` |
| Array methods | LINQ |
| `undefined` / `null` | Nullable Type |
| Express/Nest Middleware | ASP.NET Core Middleware |
| NestJS Provider | ASP.NET Core DI Service |

### 项目练习

- 创建 `Post`、`User`、`PostStatus`。
- 实现文章创建、修改标题和发布规则。
- 使用 LINQ 对文章集合过滤、排序和分页。
- 为发布规则编写单元测试。

### 必须能回答

- `class` 和 `record` 有什么区别？
- `IEnumerable<T>` 是什么？
- `async/await` 是否会自动创建新线程？
- 为什么 Web API 方法要传递 `CancellationToken`？
- 为什么不能随意使用 `.Result` 或 `.Wait()`？

### 完成标准

- [ ] 能独立编写一个异步 C# 方法。
- [ ] 能使用 LINQ 完成过滤、排序和映射。
- [ ] 至少完成 5 个领域规则单元测试。

## 8. 第 2 周：ASP.NET Core Minimal API

### 必须学习

- `WebApplicationBuilder` 和 `WebApplication`。
- Request、Response 和 HTTP 状态码。
- Route Group。
- Dependency Injection 生命周期。
- Middleware 执行顺序。
- DTO 与数据库 Entity 的区别。
- Request Validation。
- `ProblemDetails`。
- Configuration 和 Options Pattern。
- OpenAPI。

### 项目练习

- 使用 `MapGroup("/api/posts")` 组织路由。
- 暂时使用内存集合实现文章 CRUD。
- 为创建和更新接口添加验证。
- 使用统一异常处理返回 `ProblemDetails`。
- 给 API 添加 OpenAPI 文档。

### DI 生命周期

| 生命周期 | 含义 | 常见用途 |
|---|---|---|
| Singleton | 应用期间只有一个实例 | 无状态共享服务、AWS SDK Client |
| Scoped | 每个请求一个实例 | `DbContext`、业务 Service |
| Transient | 每次解析创建新实例 | 轻量无状态对象 |

### 完成标准

- [ ] 所有 CRUD Endpoint 可以通过 HTTP 调用。
- [ ] 输入错误返回 400 和统一错误结构。
- [ ] 不存在的文章返回 404。
- [ ] 创建成功返回 201 和资源地址。
- [ ] 能画出请求经过 Middleware 和 Endpoint 的顺序。

## 9. 第 3 周：PostgreSQL、SQL 与 EF Core

### 必须学习的 SQL

- `SELECT`、`INSERT`、`UPDATE`、`DELETE`。
- `WHERE`、`ORDER BY`、`LIMIT`。
- `INNER JOIN` 和 `LEFT JOIN`。
- Primary Key 和 Foreign Key。
- Unique Constraint。
- Index。
- Transaction。

### 必须学习的 EF Core

- `DbContext` 和 `DbSet<T>`。
- Entity Configuration。
- Migration。
- 一对多关系。
- 异步 LINQ 查询。
- `IQueryable<T>`。
- Change Tracking 和 `AsNoTracking()`。
- `Include()` 与 Projection。
- Transaction。
- 基础分页。

### 项目练习

- 使用 Docker Compose 启动 PostgreSQL。
- 建立 `Users` 和 `Posts` 表。
- `User` 与 `Post` 建立一对多关系。
- 用 EF Core 替换内存集合。
- 创建并应用第一份 Migration。
- 给 `Slug` 建立 Unique Index。
- 列表查询使用 `AsNoTracking()` 和分页。

### 建议命令

```bash
dotnet ef migrations add InitialCreate \
  --project src/LaoyuBlog.Api

dotnet ef database update \
  --project src/LaoyuBlog.Api
```

### 必须能回答

- `IEnumerable` 和 `IQueryable` 有什么区别？
- EF Core 什么时候真正执行 SQL？
- `AsNoTracking()` 适合什么场景？
- Migration 解决什么问题？
- N+1 Query 是什么？
- 为什么数据库约束不能只靠 API Validation？

### 完成标准

- [ ] 删除并重启 API 后，数据仍然存在。
- [ ] 能查看 EF Core 生成的 SQL。
- [ ] Slug 重复时返回 409。
- [ ] 数据库密码没有提交到 Git。
- [ ] 能手写至少 5 条对应业务的 SQL 查询。

## 10. 第 4 周：API 完整性与错误处理

### 学习内容

- REST 资源设计。
- DTO Mapping。
- Pagination、Filter 和 Sort。
- 幂等性基础。
- 统一异常处理。
- Structured Logging。
- Health Check。
- CORS。

### 项目练习

- 完成按 Slug 查询。
- 完成草稿和发布状态。
- 列表支持分页和状态过滤。
- 使用 `ILogger<T>` 输出结构化日志。
- 添加 `/health`。
- 添加数据库 Health Check。

### 完成标准

- [ ] API 状态码使用正确。
- [ ] 日志包含 PostId、UserId 和 TraceId 等结构化字段。
- [ ] `/health` 能检测 API 和数据库状态。
- [ ] 列表接口不会一次返回全部数据。

## 11. 第 5 周：JWT、授权和测试

### Authentication 与 Authorization

- Authentication：确认用户是谁。
- Authorization：判断用户能做什么。

### 必须学习

- 密码 Hash；绝不保存明文密码。
- JWT Access Token。
- Claims。
- Role 与 Policy 基础。
- 401 与 403 的区别。
- `[Authorize]` 或 `RequireAuthorization()`。
- xUnit。
- Mock、Fake 和真实依赖的边界。
- `WebApplicationFactory` 集成测试。

### 项目练习

- 注册用户。
- 登录并签发 JWT。
- 创建、更新和删除文章需要登录。
- 只有文章作者可以修改自己的文章。
- 为 Auth 和 Posts 编写集成测试。

### 重点测试场景

- [ ] 正常注册和登录。
- [ ] 重复邮箱注册失败。
- [ ] 未登录创建文章返回 401。
- [ ] 修改他人文章返回 403。
- [ ] 查询不存在文章返回 404。
- [ ] Slug 冲突返回 409。
- [ ] 无效输入返回 400。

### 完成标准

- [ ] 至少 10 个单元测试。
- [ ] 至少 8 个 API 集成测试。
- [ ] 测试可以通过一个命令运行。
- [ ] 能解释哪些测试应该 Mock、哪些应该使用真实数据库。

## 12. 第 6 周：Docker 与本地生产化

### 必须学习

- Image 与 Container。
- Dockerfile。
- Multi-stage Build。
- Docker Compose。
- Environment Variable。
- Volume。
- Container Network。
- Health Check。
- Linux 基础命令和日志查看。

### 项目练习

- 为 ASP.NET Core 编写 Multi-stage Dockerfile。
- 使用 Docker Compose 启动 API 和 PostgreSQL。
- API 不依赖本地开发机文件。
- 配置从环境变量读取。
- 上传图片不写入 Container 文件系统。

### 完成标准

- [ ] 新电脑只需 Docker 和项目代码即可启动。
- [ ] `docker compose up` 能启动完整后端。
- [ ] 删除 API Container 后重新创建，数据库数据仍存在。
- [ ] Secret 不存在于 Image 或 Git 历史中。

## 13. 第 7 周：部署单台 EC2

### AWS 范围

只学习：

- EC2。
- IAM Role。
- Security Group。
- EBS。
- CloudWatch Logs。
- Route 53 和 HTTPS 基础。
- AWS Budgets。

暂时不创建：

- ECS。
- ALB。
- RDS。
- NAT Gateway。
- Kubernetes/EKS。

### 推荐结构

```text
Internet
  ↓ 80/443
EC2
├── Reverse Proxy
├── ASP.NET Core Container
└── PostgreSQL Container + EBS Volume
```

### 安全要求

- PostgreSQL 的 5432 不向互联网开放。
- SSH 只允许自己的 IP，或者使用 Session Manager。
- EC2 使用 IAM Role，不保存长期 AWS Access Key。
- 设置 AWS Budget 告警。
- PostgreSQL 数据定期备份。

### 完成标准

- [ ] 公网 HTTPS 可以访问 API。
- [ ] 重启 EC2 后服务可以恢复。
- [ ] CloudWatch 能看到应用日志。
- [ ] 数据库端口未公开。
- [ ] 能从备份恢复 PostgreSQL。
- [ ] 能估算并解释 EC2、EBS 和公共 IPv4 费用。

## 14. 第 8 周：S3 + Lambda 小型练习

### 目标

保留传统 ASP.NET Core 主 API，只用 Lambda 学习事件驱动：

```text
API 获取预签名上传地址
        ↓
浏览器上传图片到 S3
        ↓
S3 ObjectCreated Event
        ↓
Lambda Worker
        ↓
记录图片信息或生成缩略图
```

### 必须学习

- S3 Bucket 和 Object Key。
- Presigned URL。
- Lambda Handler。
- S3 Event Payload。
- Lambda Execution Role。
- 最小 IAM 权限。
- CloudWatch Logs。
- 重试和幂等性基础。

### 项目练习

- API 生成 S3 Presigned URL。
- 浏览器不经过 API Server 直接上传 S3。
- S3 创建对象后触发 Lambda。
- Lambda 输出结构化日志。
- 同一个事件重复执行不会产生错误结果。

### 完成标准

- [ ] 前端能直接上传图片到 S3。
- [ ] Lambda 能收到并解析 S3 Event。
- [ ] IAM Policy 只允许需要的 Bucket 和操作。
- [ ] 能在 CloudWatch 中定位一次执行。
- [ ] 能解释冷启动、热启动和事件重试。

## 15. 第 9 周：整理、面试与项目展示（可选）

### README 必须包含

- 项目解决什么问题。
- 架构图。
- 技术栈。
- 本地运行步骤。
- 环境变量说明。
- Migration 步骤。
- 测试命令。
- AWS 部署说明。
- API 示例。
- 安全和成本注意事项。

### 必须能演示

```text
注册 → 登录 → 创建文章 → 发布文章 → 上传图片
```

### 必须能解释

- 一个 HTTP 请求如何经过 ASP.NET Core。
- 为什么 `DbContext` 通常注册为 Scoped。
- EF Core 如何生成和执行 SQL。
- 为什么读取列表时使用 `AsNoTracking()`。
- JWT 的认证和授权有什么区别。
- Docker Image 如何部署到 EC2。
- EC2 上的数据为什么需要 Volume 和备份。
- S3 为什么适合保存图片。
- Lambda 为什么适合异步图片任务。
- 如果将来扩容，怎样迁移到 RDS、ECS 和 ALB。

## 16. 当前必须掌握与以后再学

### 现在必须掌握

- C# 类型、接口、泛型和 LINQ。
- `async/await` 和 `CancellationToken`。
- ASP.NET Core Minimal API。
- DI 和 Middleware。
- REST、Validation 和 `ProblemDetails`。
- SQL、PostgreSQL 和 EF Core。
- JWT 认证授权。
- xUnit 单元测试和集成测试。
- Docker 和 Docker Compose。
- EC2、Security Group、IAM、S3、Lambda、CloudWatch。

### 只需知道概念

- ECS：托管和扩展 Container。
- ALB：把流量分发给多个实例或 Task。
- RDS：托管关系数据库。
- Auto Scaling：根据负载调整实例数量。
- SQS：解耦和缓冲异步任务。

### 暂缓学习

- Kubernetes/EKS。
- 微服务拆分。
- Event Sourcing。
- 复杂 DDD。
- CQRS/MediatR 大量模板代码。
- Kafka。
- Service Mesh。
- 多区域容灾。
- 高并发性能调优。

## 17. 防止学习范围失控的规则

遇到新技术时，先问三个问题：

1. 当前博客功能是否真的需要它？
2. 它是否属于目标岗位的高频要求？
3. 不学它是否会阻止当前版本完成？

如果三个答案都是“否”，记录到 Backlog，但当前不学。

每周最多引入一个重要新概念。例如学习 EF Core 的一周，不同时引入 Redis、消息队列和 Clean Architecture。

## 18. 每周复盘模板

```markdown
## Week N

### 本周完成
- 

### 我能解释的概念
- 

### 遇到的问题与原因
- 

### 下周只做三件事
1. 
2. 
3. 

### 暂缓项
- 
```

## 19. 第一版总完成标准

只有满足以下条件，第一版才算完成：

- [ ] 项目可以从空数据库完成 Migration 并启动。
- [ ] 注册、登录、文章 CRUD、发布和图片上传可用。
- [ ] 受保护接口正确返回 401/403。
- [ ] API 返回统一错误格式。
- [ ] 单元测试和集成测试全部通过。
- [ ] Docker Compose 可以启动完整本地环境。
- [ ] 单台 EC2 可以通过 HTTPS 访问。
- [ ] PostgreSQL 数据有备份和恢复记录。
- [ ] 图片保存在 S3，而不是 EC2 本地目录。
- [ ] S3 Event 能触发 Lambda。
- [ ] AWS Budget 告警已经配置。
- [ ] README 足以让其他开发者运行项目。
- [ ] 能在 10 分钟内完整演示并解释架构。

## 20. 后续升级路线

第一版完成并且确实需要扩展时，再按以下顺序升级：

```text
1. EC2 内 PostgreSQL → RDS PostgreSQL
2. Docker Image → ECR
3. EC2 上的 API Container → ECS Fargate
4. 在 ECS 前增加 ALB
5. 一个 ECS Task → 两个 Task + Auto Scaling
6. 需要缓存时再增加 Redis/ElastiCache
7. 需要异步任务时再增加 SQS Worker
```

升级前保持以下约束：

- 应用无状态。
- 图片存 S3。
- 配置使用环境变量。
- Secret 不进入代码仓库。
- 日志输出到标准输出。
- 提供 `/health`。
- 不依赖固定服务器 IP。
- 数据库 Migration 独立执行。

## 21. 官方参考资料

- [.NET 文档](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core 文档](https://learn.microsoft.com/aspnet/core/)
- [Minimal API 文档](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [EF Core 文档](https://learn.microsoft.com/ef/core/)
- [ASP.NET Core 集成测试](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- [AWS .NET 开发者中心](https://aws.amazon.com/developer/language/net/)
- [AWS EC2 文档](https://docs.aws.amazon.com/ec2/)
- [AWS Lambda .NET 文档](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html)
- [AWS S3 文档](https://docs.aws.amazon.com/s3/)

---

执行原则：先完成一个小而完整的系统，再扩大技术范围。当前目标是成为“能够独立完成并部署 .NET 后端的前端开发者”，不是一次性成为后端、云架构和 DevOps 专家。
