# 第三部分：ASP.NET Core Web API（46～75）

## 46. ASP.NET Core request pipeline 是什么？【P0】

**中文解释：** 请求管道是按注册顺序组成的一系列 request delegate。每个 middleware 可以在调用下一个组件前后执行逻辑，也可以短路请求直接返回。响应通常以相反方向穿过已执行的 middleware。

**English answer:** The ASP.NET Core request pipeline is an ordered chain of request delegates. Each middleware can run code before and after the next component, or short-circuit the pipeline. This makes registration order significant.

## 47. middleware 是什么？【P0】

**中文解释：** Middleware 是处理 HTTP 请求和响应的管道组件，常用于异常处理、日志、HTTPS、CORS、认证和授权。它适合跨多个 endpoint 的横切逻辑；具体 action 的业务规则不应塞进 middleware。

**English answer:** Middleware is a component in the HTTP pipeline that processes requests and responses. It is appropriate for cross-cutting concerns such as exception handling, logging, CORS, and authentication, rather than endpoint-specific business logic.

## 48. middleware 顺序为什么重要？【P0】

**中文解释：** 每个组件只能作用于它之后的组件，并在响应回程执行后置逻辑。例如异常处理要靠前才能捕获后续异常；认证应在授权之前；CORS 必须放在正确位置才能给响应添加头。顺序错误会造成安全或行为问题。

**English answer:** Middleware order defines which components wrap others. Exception handling belongs early, authentication must run before authorization, and CORS must be placed where it can handle the response. Incorrect order can change both behavior and security.

## 49. Dependency Injection 是什么？【P0】

**中文解释：** DI 让对象从外部获得依赖，而不是在内部 `new` 具体实现。ASP.NET Core 内置容器负责注册、解析和管理服务生命周期，可降低耦合、便于测试和替换实现。不是每个简单类都必须抽象成接口。

**English answer:** Dependency injection supplies an object's dependencies from the outside instead of constructing concrete implementations internally. ASP.NET Core's container manages registrations and lifetimes, improving separation and testability.

## 50. Transient、Scoped、Singleton 有什么区别？【P0】

**中文解释：** Transient 每次解析创建新实例；Scoped 在一次 HTTP 请求范围内复用同一实例；Singleton 在应用进程生命周期内复用。Singleton 必须线程安全，也不能直接持有 Scoped 服务；`DbContext` 通常使用 Scoped。

**English answer:** Transient creates a new instance each time, scoped reuses one instance per request, and singleton reuses one instance for the application lifetime. Singletons must be thread-safe and must not directly capture scoped services.

## 51. 为什么 DbContext 通常是 Scoped？【P0】

**中文解释：** 一个 HTTP 请求通常对应一个 unit of work：查询、跟踪变更、保存，然后释放。Scoped 正好把这些操作放在一个短生命周期中。`DbContext` 不是线程安全的，Singleton 会共享状态并导致并发问题。

**English answer:** A scoped DbContext matches one unit of work per HTTP request. It can query, track changes, save, and then be disposed. DbContext is not thread-safe, so sharing one singleton instance across requests would be unsafe.

## 52. Controller 和 Minimal API 有什么区别？【P1】

**中文解释：** Controller 使用属性、约定、过滤器和 `ControllerBase` 组织较完整的 API；Minimal API 用更少样板直接映射 endpoint，适合小型服务和轻量接口。两者共享同一 ASP.NET Core 基础设施，选择主要取决于规模、组织方式和团队偏好。

**English answer:** Controllers provide a structured model with attributes, conventions, and filters. Minimal APIs map endpoints with less ceremony. Both use the same ASP.NET Core platform, so I choose based on application size and organizational needs.

## 53. `[ApiController]` 有什么作用？【P0】

**中文解释：** 它启用面向 API 的约定，包括自动 HTTP 400、参数绑定来源推断和更一致的问题详情响应等。使用它时，模型验证失败通常在 action 执行前自动返回，因此不必每个 action 手写相同的 `ModelState` 判断。

**English answer:** ApiController enables API-focused conventions such as binding-source inference and automatic 400 responses for invalid models. It removes repetitive ModelState checks and produces more consistent client errors.

## 54. attribute routing 是什么？【P0】

**中文解释：** 通过 `[Route]`、`[HttpGet]`、`[HttpPost]` 等把 URL 模板和 HTTP method 声明在 Controller 或 action 上。Controller 路由与 action 路由通常组合，例如 `[Route("api/blogs")]` 加 `[HttpGet("{id:int}")]`。

**English answer:** Attribute routing declares route templates and HTTP methods directly on controllers and actions. Controller and action templates can combine, and route constraints such as int help disambiguate matches.

## 55. conventional routing 和 attribute routing 怎么选？【P1】

**中文解释：** Conventional routing 通过统一模板推断 controller/action，常用于 MVC 页面；attribute routing 对每个 endpoint 更显式，通常更适合 REST Web API。项目可以同时使用，但 API 路由应保持一致和可预测。

**English answer:** Conventional routing applies shared route patterns and is common in MVC applications. Attribute routing is explicit per endpoint and is usually a better fit for REST APIs. Consistency matters more than mixing styles without a reason.

## 56. model binding 是什么？【P0】

**中文解释：** 模型绑定从 route、query string、form、header 或 request body 中读取数据，并转换成 action 参数或对象。绑定成功不代表业务有效，还需要 validation。复杂 body 通常由 input formatter 从 JSON 反序列化。

**English answer:** Model binding reads request data from sources such as routes, query strings, headers, forms, and bodies, then converts it into action parameters. Binding creates the model; validation decides whether its values are acceptable.

## 57. `[FromRoute]`、`[FromQuery]`、`[FromBody]` 有什么作用？【P0】

**中文解释：** 它们显式指定参数的数据来源，减少歧义。资源标识通常来自 route，筛选分页来自 query，请求 DTO 来自 body。HTTP 请求体通常只能被读取并绑定一次，所以不要设计多个 `[FromBody]` 参数。

**English answer:** These attributes explicitly select the binding source. Resource identifiers usually come from the route, filters and pagination from the query string, and a request DTO from the body. An action should normally have only one body-bound parameter.

## 58. DTO 和 Entity 为什么要分开？【P0】

**中文解释：** Entity 面向数据库持久化，DTO 面向 API 契约。分开可以避免过度绑定、泄露内部字段、循环序列化，并让数据库模型和外部 API 独立演进。小型原型可暂时直接返回实体，但生产接口通常应明确 DTO。

**English answer:** Entities model persistence, while DTOs model the public API contract. Separating them prevents over-posting and accidental data exposure, and allows the database and API to evolve independently.

## 59. `IActionResult`、`ActionResult<T>` 和直接返回 T 怎么选？【P0】

**中文解释：** 只会成功并返回固定数据时可直接返回 T；需要多种 HTTP 结果时用 `IActionResult`；既要强类型成功结果又要返回 NotFound、BadRequest 等时，`ActionResult<T>` 最方便。清晰表达 endpoint 的可能结果最重要。

**English answer:** I return T for a simple fixed success result, IActionResult for multiple untyped HTTP outcomes, and ActionResult<T> when I want a typed success body plus results such as NotFound or BadRequest.

## 60. `CreatedAtAction` 为什么比直接返回 201 更好？【P1】

**中文解释：** 它返回 201 Created，并根据指定 action 生成新资源的 `Location` header，同时可返回资源表示。这符合 REST 创建语义，也避免手工拼 URL。前提是路由参数能够正确匹配目标 action。

**English answer:** CreatedAtAction returns 201 and generates a Location header pointing to the new resource. It communicates REST creation semantics and avoids hard-coded URLs, provided the target route values are correct.

## 61. model validation 怎么工作？【P0】

**中文解释：** Data Annotations 或自定义验证器检查绑定后的模型，例如 `[Required]`、`[Range]`。在 `[ApiController]` 下，无效模型默认自动返回 400。格式验证之外的业务规则，如 slug 唯一性，通常应在业务或数据访问逻辑中检查。

**English answer:** Model validation checks the bound model using attributes or custom validators. With ApiController, invalid models normally produce an automatic 400 response. Database-dependent business rules belong beyond simple input validation.

## 62. authentication 和 authorization 有什么区别？【P0】

**中文解释：** Authentication 回答“你是谁”，验证身份并建立 `ClaimsPrincipal`；Authorization 回答“你是否能做这件事”，根据角色、claim、policy 或资源判断权限。必须先认证，授权才有身份信息可用。

**English answer:** Authentication establishes who the user is. Authorization decides whether that user may perform a specific action. Authentication provides the identity and claims that authorization evaluates.

## 63. JWT authentication 基本流程是什么？【P0】

**中文解释：** 用户通过登录验证后获得签名的 access token；客户端随后在 `Authorization: Bearer` header 中发送它；服务器验证签名、issuer、audience、有效期等，再从 claims 建立身份。JWT 通常是签名而非加密，不能放秘密数据。

**English answer:** After login, the client receives a signed access token and sends it as a Bearer token. The API validates the signature, issuer, audience, and lifetime, then creates an identity from the claims. A JWT is usually signed, not encrypted.

## 64. 401 和 403 有什么区别？【P0】

**中文解释：** 401 表示请求没有有效身份认证，例如缺少或无效 token；403 表示身份已确认，但没有访问该资源的权限。不要把“找不到资源”和“没有权限”都随意返回 401。

**English answer:** A 401 means the request is not successfully authenticated. A 403 means the user is authenticated but lacks permission. The distinction helps clients decide whether to re-authenticate or stop retrying.

## 65. role-based 和 policy-based authorization 有什么区别？【P1】

**中文解释：** Role-based 直接检查用户角色，简单但表达能力有限；Policy-based 把一个或多个 requirement 和 handler 组合起来，可以检查 claims、业务条件或资源所有权。复杂权限通常用 policy 更清晰、更可测试。

**English answer:** Role-based authorization checks membership in named roles. Policy-based authorization composes requirements and handlers, allowing richer claim or resource checks. I prefer policies when permissions go beyond a simple role name.

## 66. access token 和 refresh token 有什么区别？【P0】

**中文解释：** access token 生命周期短，用来访问 API；refresh token 生命周期更长，用来换取新的 access token，应安全保存、可撤销并考虑轮换。refresh token 不应该直接拿来调用普通业务 endpoint。

**English answer:** An access token is short-lived and authorizes API calls. A refresh token is longer-lived and is used only to obtain new access tokens. Refresh tokens require secure storage, revocation, and often rotation.

## 67. CORS 是什么？【P0】

**中文解释：** CORS 是浏览器执行的跨源访问规则，服务器通过响应头声明允许哪些 origin、method 和 header。它不是 API 身份认证，也不能阻止 curl 或服务器程序调用接口。带 credential 时不能把允许来源简单设置为 `*`。

**English answer:** CORS is a browser-enforced policy controlled by server response headers. It determines which web origins may read cross-origin responses. It is not authentication and does not block non-browser clients.

## 68. ASP.NET Core configuration 从哪里来？【P0】

**中文解释：** 配置可由 `appsettings.json`、环境专用 JSON、环境变量、命令行和 secret provider 等提供，后添加的 provider 通常覆盖前面的值。结构化配置推荐绑定到 Options 类，并验证关键配置。

**English answer:** ASP.NET Core configuration combines providers such as JSON files, environment variables, command-line arguments, and secret stores. Later providers can override earlier ones. I bind related settings to validated options classes.

## 69. 为什么不能把 secret 提交到 appsettings.json？【P0】

**中文解释：** 仓库历史、构建产物和日志都可能泄露密钥，而且删除当前文件并不会清除 Git 历史。开发时可用 user secrets 或本地环境变量，部署时使用平台 secret store，并在泄露后立即轮换。

**English answer:** Secrets committed to configuration files can leak through repository history and build artifacts. I use local secret storage during development and a managed secret store or protected environment variables in production, with rotation after exposure.

## 70. logging 应该记录什么，不应该记录什么？【P0】

**中文解释：** 应记录结构化事件、request/correlation ID、关键业务结果、耗时和异常上下文；不应记录密码、token、完整信用卡号或不必要的个人数据。使用参数化模板而不是字符串拼接，方便查询和保留结构。

**English answer:** I log structured events with correlation identifiers, outcomes, timing, and useful exception context. I never log passwords, tokens, or unnecessary personal data. Message templates preserve searchable fields.

## 71. global exception handling 怎么做？【P0】

**中文解释：** 把异常处理 middleware 放在管道前部，捕获后续未处理异常，记录内部详情，并向客户端返回统一且不泄露堆栈的 Problem Details。已知领域异常可映射到 400、404、409 等，未知异常通常为 500。

**English answer:** I place centralized exception handling early in the pipeline. It logs internal details and maps known exceptions to appropriate Problem Details responses, while unexpected exceptions return a safe 500 without exposing stack traces.

## 72. filter 和 middleware 有什么区别？【P1】

**中文解释：** Middleware 工作在整个 HTTP 管道，可覆盖所有 endpoint；MVC filter 工作在 Controller/action 执行流程中，可以访问 action 参数、ModelState 和 result。通用 HTTP 横切逻辑用 middleware，Controller 特定行为才考虑 filter。

**English answer:** Middleware operates across the HTTP pipeline and can cover all endpoint types. MVC filters run inside the controller action pipeline and understand action arguments and results. The required scope determines which one I use.

## 73. health check 有什么用？【P1】

**中文解释：** Health check 暴露应用是否存活、是否准备好接收流量的信号，部署平台可据此路由或重启实例。Liveness 不应依赖每个外部服务；readiness 可检查关键依赖，但要设置合理超时，避免检查本身放大故障。

**English answer:** Health checks expose liveness and readiness signals to deployment infrastructure. Liveness answers whether the process should be restarted, while readiness answers whether it can receive traffic. Dependency checks should be bounded and purposeful.

## 74. integration test 和 unit test 在 Web API 中分别测什么？【P0】

**中文解释：** Unit test 隔离测试业务方法和边界条件，速度快；integration test 通过测试服务器验证路由、绑定、中间件、序列化和依赖集成。Controller API 不能只测 service，因为很多错误发生在 HTTP 管道配置上。

**English answer:** Unit tests isolate business logic and edge cases. Integration tests exercise routing, model binding, middleware, serialization, and dependency wiring through the HTTP pipeline. A reliable API test suite usually needs both.

## 75. Kestrel 和 reverse proxy 是什么关系？【P1】

**中文解释：** Kestrel 是 ASP.NET Core 的跨平台 Web server。生产环境可直接对外，也常放在 Nginx、IIS、云负载均衡或 ingress 后面，由代理处理 TLS、公共入口、转发和部分安全策略。转发头必须正确配置，应用才能识别原始 scheme 和客户端信息。

**English answer:** Kestrel is the cross-platform web server used by ASP.NET Core. It can be internet-facing, but it is often placed behind a reverse proxy or load balancer for TLS termination and traffic management. Forwarded headers must be configured carefully.

