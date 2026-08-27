# ASP.NET Core（R026～R055）

> 每题按“口述 → 追问/失分点 → English”练习。

## R026. 一个请求从 Kestrel 到 Controller 经历什么？
**口述：** Kestrel 接收请求，按顺序经过 middleware，路由选择 endpoint，再做绑定、验证、action 执行和结果序列化。  
**追问/失分点：** 响应如何回程？不要漏掉 middleware 顺序。  
**英文口述：** Kestrel accepts the request, which passes through ordered middleware. Routing selects an endpoint, then model binding, validation, action execution, and result serialization produce the response.  
**英文追问回答：** The response travels back through middleware that called the next delegate, so middleware order affects both request and response behavior.

## R027. Middleware 顺序错了会发生什么？
**口述：** 异常处理要包住后续组件，认证在授权前，CORS 要处于能处理响应的位置；顺序会改变功能和安全。  
**追问/失分点：** 举一个 401/CORS 错误；不要说顺序无所谓。  
**英文口述：** Exception handling must wrap later components, authentication must run before authorization, and CORS must be placed where it can add the correct headers. Order affects behavior and security.  
**英文追问回答：** Incorrect auth order can prevent authorization from seeing a user; incorrect CORS placement can omit headers even when the endpoint succeeds.

## R028. 你会怎样实现全局异常处理？
**口述：** 管道前部统一捕获，内部记录完整异常，对外返回不泄密的 Problem Details；已知错误映射 400/404/409，未知错误 500。  
**追问/失分点：** correlation ID？不要返回 stack trace。  
**英文口述：** I handle unhandled exceptions near the start of the pipeline, log full internal details, and return safe Problem Details. Known failures map to 400, 404, or 409, while unexpected failures return 500.  
**英文追问回答：** A correlation ID links the client response to logs; stack traces and internal exception details must not be exposed.

## R029. .NET DI 是什么？项目里怎么用？
**口述：** 在 Program.cs 注册映射，Controller 通过构造函数声明依赖，由容器创建对象并管理生命周期；价值是解耦和可测试性。  
**追问/失分点：** 不要只回答“不用 new”；说明真实 service。  
**英文口述：** I register service mappings in `Program.cs`, declare dependencies in a controller constructor, and let the container create objects and manage lifetimes. This reduces coupling and improves testability.  
**英文追问回答：** For example, a controller depends on `IBlogService` rather than constructing its database and business dependencies itself.

## R030. Transient、Scoped、Singleton 怎么选？
**口述：** Transient 每次解析新建；Scoped 通常每请求一个；Singleton 全应用共享且须线程安全。选择依据是状态和资源边界。  
**追问/失分点：** DbContext 为什么 Scoped？Singleton 不能持有 Scoped。  
**英文口述：** Transient creates an instance per resolution, scoped normally reuses one per request, and singleton shares one for the application lifetime. I choose based on state, ownership, and concurrency.  
**英文追问回答：** DbContext is scoped to one request-sized unit of work; a singleton must not capture a scoped dependency.

## R031. Singleton 依赖 Scoped 为什么有问题？
**口述：** 长生命周期对象会捕获短生命周期实例，使它跨请求共享或无法按时释放，称 captive dependency；应重设边界或按 scope 解析。  
**追问/失分点：** scope validation 能发现什么？不要直接注入 DbContext。  
**英文口述：** A singleton that captures a scoped service extends that instance beyond its intended request, which is a captive dependency. I redesign the lifetime boundary or create an explicit scope when appropriate.  
**英文追问回答：** Scope validation detects some invalid lifetime graphs; directly injecting DbContext into a singleton is unsafe.

## R032. Middleware 能在构造函数注入 DbContext 吗？
**口述：** 普通 middleware 通常启动时创建，不能构造注入 scoped DbContext；应注入 `InvokeAsync` 参数，按请求解析。  
**追问/失分点：** `IMiddleware` 有何不同？  
**英文口述：** Convention-based middleware is normally created at startup, so I do not constructor-inject a scoped DbContext. I inject it into `InvokeAsync` so it is resolved per request.  
**英文追问回答：** `IMiddleware` can be activated per request through the container, so its dependency behavior differs from conventional middleware.

## R033. BackgroundService 需要 DbContext 怎么办？
**口述：** Hosted service 是 singleton，应通过 `IServiceScopeFactory` 每轮创建 scope，解析 DbContext，用完释放；或用 DbContextFactory。  
**追问/失分点：** 不要把请求 Scoped context 保存到字段。  
**英文口述：** A hosted service is singleton, so each unit of background work creates a scope through `IServiceScopeFactory` and resolves a DbContext inside it, or uses a context factory.  
**英文追问回答：** I never store a request-scoped DbContext in a background-service field because it would outlive and cross its request boundary.

## R034. `[ApiController]` 给你带来什么？
**口述：** 提供绑定来源推断、自动 400 和一致验证错误等 API 约定，减少重复 ModelState 判断。  
**追问/失分点：** 业务唯一性是否属于 DataAnnotations？  
**英文口述：** `ApiController` enables API conventions such as binding-source inference, automatic 400 responses, and consistent validation errors, reducing repeated ModelState checks.  
**英文追问回答：** Data annotations handle input shape; database-dependent rules such as slug uniqueness belong in application and database logic.

## R035. `[FromRoute]`、`[FromQuery]`、`[FromBody]` 怎么选？
**口述：** 资源身份放 route，筛选分页放 query，复杂请求 DTO 放 body；一般只有一个 body 参数。  
**追问/失分点：** GET body 为什么不合适？  
**英文口述：** I put resource identity in route values, filtering and pagination in query parameters, and a complex request representation in the body. An action normally has one body-bound parameter.  
**英文追问回答：** A GET body has poor interoperability and caching semantics, so retrieval inputs normally belong in the route or query string.

## R036. DTO 为什么不能直接换成 Entity？
**口述：** DTO 保护 API 契约，避免 over-posting、内部字段泄露和数据库结构耦合；请求与响应也可分开。  
**追问/失分点：** 小原型何时可简化？不要空谈 AutoMapper。  
**英文口述：** DTOs protect the external API contract from the persistence model, prevent over-posting and internal-field exposure, and let request and response shapes evolve separately.  
**英文追问回答：** A tiny prototype may simplify temporarily, but the boundary becomes valuable once security or independent evolution matters; mapping tools do not replace that design decision.

## R037. `ActionResult<T>` 什么时候比 `IActionResult` 好？
**口述：** 成功有强类型 body，同时可能返回 NotFound/BadRequest 时使用；只返回单一结果可直接 T。  
**追问/失分点：** OpenAPI 如何受益？  
**英文口述：** I use `ActionResult<T>` when success returns a typed body but the action can also return outcomes such as NotFound or BadRequest. A single fixed success result can return `T` directly.  
**英文追问回答：** The typed success contract improves generated OpenAPI information while preserving alternate HTTP results.

## R038. 创建资源为什么用 `CreatedAtAction`？
**口述：** 返回 201、资源表示及 Location header，并从路由生成 URL，避免手拼。  
**追问/失分点：** 路由参数不匹配会怎样？不要返回 200 代替 201。  
**英文口述：** `CreatedAtAction` returns 201, the created representation, and a generated Location header instead of hard-coding the URL.  
**英文追问回答：** Its action name and route values must match the target route; creation should not be reported as a generic 200 response.

## R039. PUT 与 PATCH 在 Controller 中怎样体现？
**口述：** PUT 表示完整替换并应幂等；PATCH 表示部分修改，要区分缺失字段和显式 null 并限制可修改字段。  
**追问/失分点：** 重复请求结果？不要把二者都实现成同一 Update DTO。  
**英文口述：** PUT normally replaces a complete representation and is idempotent. PATCH models partial change, distinguishes omitted fields from explicit null, and restricts which fields may change.  
**英文追问回答：** Repeating the same PUT should have the same final effect; using one identical update DTO for PUT and PATCH often loses their distinct semantics.

## R040. 401 与 403 分别什么时候返回？
**口述：** 401 是没有有效认证身份，403 是已认证但权限不足。  
**追问/失分点：** token 过期？不要用 401 表示所有安全错误。  
**英文口述：** 401 means the request has no valid authenticated identity; 403 means the identity is known but lacks permission.  
**英文追问回答：** An expired token normally leads to 401; authentication and authorization failures should not all be collapsed into one code.

## R041. JWT 登录流程怎么讲？
**口述：** 登录验证后签发短期 access token，客户端 Bearer 发送，API 验证签名、issuer、audience、expiry 并建立 claims。  
**追问/失分点：** JWT 通常签名而非加密，不能存秘密。  
**英文口述：** After successful login, the server issues a short-lived access token. The client sends it as a Bearer token, and the API validates signature, issuer, audience, and expiry before building claims.  
**英文追问回答：** A JWT is normally signed rather than encrypted, so it must not contain secrets.

## R042. Refresh token 怎样安全处理？
**口述：** 它只用于换 access token，应安全存储、可撤销、设置过期并轮换；检测重复使用可撤销 token family。  
**追问/失分点：** 不要把 refresh token 当普通 API token。  
**英文口述：** A refresh token is used only to obtain new access tokens. It needs secure storage, expiration, revocation, and rotation, with reuse detection able to revoke the token family.  
**英文追问回答：** It must never be accepted as the credential for ordinary business endpoints.

## R043. Authentication 与 Authorization 有何区别？
**口述：** 前者确认身份并建立 principal，后者用 role/claim/policy 判断能否执行操作。  
**追问/失分点：** policy-based 为什么比硬编码 role 灵活？  
**英文口述：** Authentication establishes the caller's identity and claims; authorization evaluates roles, claims, policies, or resource rules to decide whether an operation is allowed.  
**英文追问回答：** Policy-based authorization keeps richer permission rules testable and avoids scattering hard-coded role checks.

## R044. CORS 能保护 API 不被 curl 调用吗？
**口述：** 不能；CORS 是浏览器读取跨源响应的规则，不是认证，也不限制非浏览器客户端。  
**追问/失分点：** credentials 与 `*`？  
**英文口述：** CORS is a browser-enforced cross-origin response policy, not authentication or network access control. It cannot prevent curl or another server from calling the API.  
**英文追问回答：** Credentialed CORS requests cannot safely use a wildcard origin; allowed origins must be explicit.

## R045. 配置值来自多个 provider，谁覆盖谁？
**口述：** 通常后添加 provider 优先，环境变量可覆盖 JSON；结构化配置绑定 Options 并在启动时验证。  
**追问/失分点：** secret 不进 Git；说出生产 secret store。  
**英文口述：** ASP.NET Core combines configuration providers, and later providers normally override earlier ones, so environment variables can override JSON. I bind related values to validated Options.  
**英文追问回答：** Secrets stay out of Git and production uses a protected secret store; critical options should fail validation at startup.

## R046. 日志为什么用模板而非字符串拼接？
**口述：** 模板保留结构化字段，便于搜索聚合且避免不必要格式化；不记录 token、密码和敏感个人数据。  
**追问/失分点：** correlation ID 如何贯穿请求？  
**英文口述：** Structured message templates preserve searchable fields and avoid unnecessary string construction. I include correlation context but never log passwords, tokens, or unnecessary personal data.  
**英文追问回答：** A correlation ID is created or accepted at the request boundary and included consistently in logs and downstream calls.

## R047. Controller 应该多薄？
**口述：** 负责 HTTP 输入输出、调用用例和状态码映射；复杂、复用或需独立测试的业务逻辑进入 service，但不为简单 CRUD强造层。  
**追问/失分点：** service layer 何时值得引入？  
**英文口述：** Controllers handle HTTP concerns, map inputs and outcomes, and invoke use cases. I move complex or reused business rules into services, but I do not add a service layer to trivial CRUD without a need.  
**英文追问回答：** A service layer becomes useful when logic is reused, controllers become difficult to test, or transactions and business rules need a clear boundary.

## R048. Filter 与 Middleware 怎么选？
**口述：** 全管道横切逻辑用 middleware；需要 action 参数、ModelState 或 MVC result 的行为用 filter。  
**追问/失分点：** 异常处理为何通常用 middleware？  
**英文口述：** Middleware handles cross-cutting behavior across the HTTP pipeline; MVC filters are appropriate when behavior needs action arguments, ModelState, or action results.  
**英文追问回答：** Global exception handling usually belongs in middleware because it can cover the assembled pipeline consistently.

## R049. API pagination 如何设计？
**口述：** query 接 page/pageSize 或 cursor，限制最大页大小，返回稳定排序和必要元数据；深分页考虑 keyset。  
**追问/失分点：** 排序字段不唯一时追加唯一键。  
**英文口述：** I accept page/pageSize or a cursor, enforce a maximum size, apply deterministic ordering, and return suitable metadata. Deep pagination may use keyset rather than offset.  
**英文追问回答：** If the main sort value is not unique, I append a unique key such as Id to make ordering stable.

## R050. 如何避免重复 POST 创建两笔订单？
**口述：** 客户端提供 idempotency key，服务端原子保存 key 与结果，重复请求返回原结果；数据库唯一约束兜底。  
**追问/失分点：** 不要只靠内存 HashSet，多实例会失效。  
**英文口述：** The client sends an idempotency key, and the server atomically persists the key with the operation result. Repeated requests return the stored result, with a database constraint as the final guard.  
**英文追问回答：** An in-memory HashSet fails across restarts and multiple instances, so it cannot guarantee idempotency.

## R051. API 为什么返回 409 而不是 400？
**口述：** 请求格式有效但与当前资源状态冲突，如唯一 slug 或并发版本冲突时用 409；一般验证错误用 400。  
**追问/失分点：** 数据库异常如何安全转换？  
**英文口述：** I return 409 when the request is valid but conflicts with current resource state, such as a unique slug or stale concurrency token. Ordinary input validation failures return 400.  
**英文追问回答：** I translate only known database constraint failures into safe domain conflicts and avoid exposing provider details.

## R052. Health check 的 liveness 与 readiness 区别？
**口述：** liveness 判断进程是否该重启，readiness 判断是否接流量；不要让 liveness 因短暂数据库失败触发重启风暴。  
**追问/失分点：** 检查依赖要设置超时。  
**英文口述：** Liveness answers whether the process should be restarted; readiness answers whether the instance should receive traffic. Temporary database failure should not create a liveness restart storm.  
**英文追问回答：** Dependency readiness checks need strict timeouts and should test only dependencies required to serve traffic.

## R053. Rate limiting 怎样设计？
**口述：** 按用户/API key/IP 或 endpoint 分区，选择 fixed/sliding/token bucket 等策略，超限返回 429；它不是完整 DDoS 防护。  
**追问/失分点：** 多实例状态放哪里？  
**英文口述：** I partition rate limits by a meaningful identity such as user, API key, IP, or endpoint and choose an appropriate algorithm. Exceeded requests return 429; rate limiting is not complete DDoS protection.  
**英文追问回答：** A multi-instance deployment needs shared or consistently partitioned rate-limit state when global enforcement is required.

## R054. Unit test 与 integration test 各测什么？
**口述：** unit 测隔离业务规则；integration 用 WebApplicationFactory 验证路由、DI、绑定、序列化和中间件。  
**追问/失分点：** 只 mock Controller 无法发现 Program.cs 配置错误。  
**英文口述：** Unit tests isolate business rules; integration tests use `WebApplicationFactory` to verify routing, DI, binding, serialization, middleware, and the HTTP contract.  
**英文追问回答：** Mocking only a controller cannot reveal configuration errors in `Program.cs` or middleware ordering.

## R055. API 线上突然变慢，第一步做什么？
**口述：** 先用指标和 trace 定位是入口、应用、数据库还是外部依赖，再查看慢请求、SQL 和资源饱和度，不先猜着重写。  
**追问/失分点：** p95/p99、correlation ID、query plan。  
**英文口述：** I first use metrics and traces to localize latency to the gateway, application, database, or external dependency. Then I inspect slow requests, SQL count and plans, and resource saturation before changing code.  
**英文追问回答：** I compare p95 and p99, correlate traces with request IDs, and verify any database hypothesis with the actual query plan.

