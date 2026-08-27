# 项目经历（R076～R090）

## R076. 用两分钟介绍你的 Web API 项目。
**口述：** 讲问题、职责、一次请求流程、关键取舍、一个难点和验证结果，不罗列技术名词。  
**追问/失分点：** 准备 30 秒与 2 分钟两版。  
**英文口述：** I introduce the problem, my responsibility, and one request flow, then explain a key trade-off, a real challenge, and the verified result. I focus on decisions rather than listing technologies.  
**英文追问回答：** I prepare a concise thirty-second version and a two-minute version so I can match the interviewer's time.

## R077. 为什么选择 Controller API 而不是 Minimal API？
**口述：** Controller 的属性路由、过滤器和组织方式适合当前接口规模；若服务很小 Minimal API 也合理。  
**追问/失分点：** 不要说其中一个“更专业”。  
**英文口述：** I chose controllers because attribute routing, filters, and their organization fit the current API size and team style. Minimal APIs would also be reasonable for a smaller service.  
**英文追问回答：** Neither style is inherently more professional; the important point is a consistent structure that fits the application.

## R078. 从 `GET /api/blogs` 讲完整调用链。
**口述：** 路由匹配、绑定分页 DTO、验证、调用查询、数据库投影/排序/分页、序列化并返回状态码。  
**追问/失分点：** 说清错误和取消如何传播。  
**英文口述：** The route selects the action, the pagination DTO is bound and validated, the query applies projection, stable ordering and pagination, and the result is serialized with the correct status.  
**英文追问回答：** Validation failures stop early, while cancellation and unexpected exceptions propagate to centralized handling.

## R079. 你项目里最值得讲的 bug 是什么？
**口述：** 用 Situation–Diagnosis–Smallest fix–Verification，提供错误证据和回归测试，不把普通功能包装成灾难。  
**追问/失分点：** 避免只说“Google 后解决”。  
**英文口述：** I describe a bug as situation, diagnostic evidence, root cause, smallest safe fix, and verification. I explain what I learned without exaggerating an ordinary issue.  
**英文追问回答：** Saying only that I searched online does not show debugging ability; I must explain the decisive evidence and regression check.

## R080. 为什么后来才引入 service layer？
**口述：** 初期 CRUD 保持简单；业务逻辑复用、Controller 变厚或需要独立测试时再提取，避免过早架构。  
**追问/失分点：** 举出具体移动的逻辑。  
**英文口述：** I kept early CRUD simple and introduced a service boundary only when logic became reused, controllers became thick, or independent testing became valuable.  
**英文追问回答：** I name the exact validation, transaction, or business rule moved out of the controller rather than defending a layer in the abstract.

## R081. API 如何处理重复 slug？
**口述：** 数据库唯一索引保证一致性，应用捕获明确冲突并返回 409；可先查改善体验但不作为保证。  
**追问/失分点：** 并发请求反例。  
**英文口述：** A database unique index guarantees slug uniqueness, and the application translates a known constraint conflict into 409. A pre-check can improve feedback but cannot prevent the race.  
**英文追问回答：** Two concurrent requests can both pass the pre-check, which is the counterexample showing why the database constraint is required.

## R082. 你如何设计分页？
**口述：** 验证 page/pageSize、限制最大值、稳定排序并返回总数或 next cursor；根据规模选择 offset/keyset。  
**追问/失分点：** CreatedAt 相同时如何排序？  
**英文口述：** I validate page and page size, enforce a maximum, apply a stable sort, and return total or cursor metadata. I choose offset or keyset based on scale and change frequency.  
**英文追问回答：** When `CreatedAt` is not unique, I append `Id` as a deterministic tie-breaker.

## R083. 为什么使用 PostgreSQL？
**口述：** 关系模型、事务、约束和成熟生态符合博客数据；选择基于需求与团队经验，不贬低其他数据库。  
**追问/失分点：** 哪些约束放数据库？  
**英文口述：** PostgreSQL matched the relational model, transaction and constraint requirements, and the team's operational context. The choice was based on project needs rather than claiming every other database is worse.  
**英文追问回答：** Uniqueness, foreign keys, and other invariants that must survive concurrency belong in database constraints.

## R084. 你怎样验证一个新增 endpoint？
**口述：** 正常、边界和错误请求；检查状态码、body、数据库副作用、日志，并补 unit/integration test。  
**追问/失分点：** 只说 Swagger 点一下不够。  
**英文口述：** I test the normal path, boundaries, and invalid requests, then verify status, body, persistence effects, and logs. I add unit or integration regression coverage at the appropriate layer.  
**英文追问回答：** Clicking an endpoint once in Swagger is useful exploration but is not sufficient verification.

## R085. 如果需求从博客扩展到评论，你先改什么？
**口述：** 先明确关系、权限和删除语义，再做模型/migration、DTO、endpoint 和测试；不直接拆微服务。  
**追问/失分点：** 文章删除时评论怎么办？  
**英文口述：** I clarify comment ownership, permissions, and deletion behavior first, then change the model, migration, DTOs, endpoints, and tests. I would not split a microservice for this feature by default.  
**英文追问回答：** The team must decide whether deleting an article cascades, restricts deletion, or preserves comments before coding the relationship.

## R086. 何时会把单体拆成微服务？
**口述：** 只有独立业务边界、团队所有权、扩缩容或发布需求产生真实收益时；先处理模块化和可观测性。  
**追问/失分点：** 不以“用户多”作为唯一理由。  
**英文口述：** I split a monolith only when a clear business boundary, independent ownership, scaling, or release requirement outweighs distributed-system cost. I improve modularity and observability first.  
**英文追问回答：** A larger user count alone does not require microservices; the actual bottleneck and ownership boundary matter.

## R087. 线上返回 500，你怎样排查？
**口述：** 用 request/correlation ID 找日志和 trace，确认版本、输入、依赖和数据库错误，在不暴露数据下复现。  
**追问/失分点：** 先恢复服务还是先找根因？  
**英文口述：** I use the request or correlation ID to connect the 500 response to logs and traces, then inspect deployment version, input, dependencies, and database errors while protecting sensitive data.  
**英文追问回答：** I first stabilize or roll back when user impact is high, preserve evidence, and continue root-cause analysis after service recovery.

## R088. 如何说明你对安全的贡献？
**口述：** 讲具体措施：DTO 防 over-posting、参数验证、secret 管理、认证授权、日志脱敏和依赖更新。  
**追问/失分点：** HTTPS 不等于应用已安全。  
**英文口述：** I explain concrete controls: DTOs against over-posting, validation, secret management, authentication and authorization, redacted logs, and dependency updates.  
**英文追问回答：** HTTPS protects transport but does not replace authorization, secure storage, validation, or safe logging.

## R089. 如果重新做一次项目，你会改什么？
**口述：** 选一个有证据的改进，如更早加集成测试或结构化日志，同时说明当时简化为何合理。  
**追问/失分点：** 不要否定整个项目。  
**英文口述：** I choose one evidence-based improvement, such as earlier integration testing or structured logging, and explain why the original simpler decision was reasonable with the information available then.  
**英文追问回答：** I avoid claiming the whole project was wrong; a strong answer demonstrates learning and trade-off awareness.

## R090. 你如何证明一个性能优化有效？
**口述：** 优化前建立基线，用相同数据负载比较 p95、SQL 次数、CPU/内存，并做回归验证。  
**追问/失分点：** 单次本地 Stopwatch 不足。  
**英文口述：** I establish a baseline and compare the same representative load before and after, using p95 latency, SQL count, CPU, memory, and regression tests.  
**英文追问回答：** A single local stopwatch measurement is too noisy and does not represent production concurrency or data volume.

