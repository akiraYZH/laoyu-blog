# 第四部分：EF Core、SQL、HTTP 与工程实践（76～100）

## 76. EF Core 是什么？【P0】

**中文解释：** EF Core 是 .NET 的对象关系映射器（ORM），把实体和关系映射到数据库表，让开发者通过 LINQ 查询并跟踪、保存对象变化。它减少样板 SQL，但不消除理解 SQL、索引、事务和查询计划的必要。

**English answer:** EF Core is an object-relational mapper for .NET. It maps entities and relationships to a database, translates LINQ queries, tracks changes, and saves them. Using an ORM does not remove the need to understand SQL and database performance.

## 77. DbContext 和 DbSet 分别是什么？【P0】

**中文解释：** `DbContext` 表示一次与数据库交互的工作单元，负责配置、查询、变更跟踪和保存；`DbSet<TEntity>` 是某种实体的查询和操作入口，类似表的代码表示，但不等于把整张表加载到内存。

**English answer:** DbContext represents a unit of work and coordinates configuration, querying, change tracking, and saving. DbSet<TEntity> is the entry point for querying and modifying one entity type; it does not mean the whole table is loaded.

## 78. EF Core migration 是什么？【P0】

**中文解释：** Migration 记录模型从一个版本到下一个版本所需的 schema 变化，并能生成升级或回退操作。应审查生成的 migration，尤其是改名、删除列和非空字段变更；生产发布通常使用受控脚本或部署流程，而不是应用启动时随意迁移。

**English answer:** A migration describes how the database schema changes between model versions. Generated migrations should be reviewed, especially for destructive or data-transforming changes. Production application requires a controlled deployment strategy.

## 79. change tracking 是什么？【P0】

**中文解释：** 跟踪查询返回实体后，DbContext 保存实体状态和原始值；`SaveChanges` 检测 Added、Modified、Deleted 等状态并生成写入。默认实体查询会跟踪，适合读取后修改；只读列表没有必要承担这部分成本。

**English answer:** Change tracking records entity states and, when needed, original values. SaveChanges uses that information to generate inserts, updates, and deletes. Tracking is useful for read-modify-write workflows but adds overhead to read-only queries.

## 80. `AsNoTracking` 有什么用？【P0】

**中文解释：** 它告诉 EF Core 不把查询出的实体加入 change tracker，通常减少只读查询的 CPU 和内存开销。适合列表和只读详情；如果之后修改实体并直接 `SaveChanges`，不会自动保存，除非重新 attach 或执行显式更新。

**English answer:** AsNoTracking disables change tracking for a query, reducing overhead for read-only work. It is a good default for read-only endpoints, but changes to those entities are not automatically persisted unless they are attached later.

## 81. `IEnumerable` 和 `IQueryable` 有什么区别？【P0】

**中文解释：** `IEnumerable<T>` 面向 .NET 内存中的枚举，后续 lambda 通常作为代码执行；`IQueryable<T>` 保存 expression tree，EF Core 可把支持的操作翻译成 SQL。在 `ToListAsync` 前保留 IQueryable 可让筛选、排序和分页在数据库执行，但不要把它随意泄露到 API 边界。

**English answer:** IEnumerable represents in-memory enumeration, while IQueryable builds an expression tree that a provider such as EF Core can translate to SQL. I compose filters before materialization, but I avoid exposing IQueryable across architectural boundaries.

## 82. `Include` 是做什么的？【P0】

**中文解释：** `Include` 和 `ThenInclude` 用于 eager loading，在查询时加载需要的导航属性。它不是每次查询都应该加；加载多个集合可能产生大结果集或笛卡尔放大，应只取需要字段并检查生成 SQL，必要时考虑 split query。

**English answer:** Include and ThenInclude eagerly load related data with the query. I only load relationships the endpoint needs, because multiple collection includes can create large result sets. Projection is often more efficient for DTO responses.

## 83. eager、explicit 和 lazy loading 有什么区别？【P0】

**中文解释：** Eager loading 在原查询中通过 Include 加载；explicit loading 之后明确调用加载某个导航；lazy loading 在访问导航时自动查询，使用方便但隐藏数据库调用。Web API 中常优先显式查询或投影，避免序列化时意外触发大量查询。

**English answer:** Eager loading fetches relationships as part of the query, explicit loading requests them later intentionally, and lazy loading fetches them when accessed. I use lazy loading cautiously because it can hide database calls and cause unpredictable API performance.

## 84. 什么是 N+1 query？【P0】

**中文解释：** 先查 N 个父对象，再为每个父对象单独查询子对象，会形成 1+N 次数据库往返。常见于循环或 lazy loading。可通过合适的 projection、Include、批量查询或重构数据访问解决，并用 SQL 日志确认，而不是只凭感觉优化。

**English answer:** N+1 occurs when one query loads parent rows and then one additional query runs for each parent. It often comes from loops or lazy loading. Projection, eager loading, or batching can solve it, and SQL logging should confirm the diagnosis.

## 85. projection 为什么常比返回完整 entity 好？【P0】

**中文解释：** `Select` 到 DTO 可只取客户端需要的列，减少网络、内存、跟踪和序列化成本，也避免泄露内部字段。投影还使查询意图清晰。不要先 `ToList` 再映射，否则数据库无法帮你减少读取列和行。

**English answer:** Projection selects only the columns needed by the API and shapes them directly into a DTO. This reduces data transfer, tracking, memory, and accidental exposure. I project before materialization so the database performs the work.

## 86. `SaveChanges` 默认有事务吗？什么时候手动事务？【P1】

**中文解释：** 对支持事务的 provider，一次 `SaveChanges` 中的多项变更通常作为事务执行，要么全部成功要么全部回滚。跨多个 `SaveChanges`、混合 EF 与其他数据库操作时可能需要显式事务，但要处理重试策略和事务边界。

**English answer:** A single SaveChanges call is transactional for providers that support transactions. I use an explicit transaction when one atomic business operation spans multiple saves or data-access mechanisms, while considering retries and failure handling.

## 87. optimistic concurrency 是什么？【P1】

**中文解释：** 乐观并发假设冲突较少，不长期锁住记录，而是在更新时通过 row version 或 concurrency token 判断数据是否被别人修改。冲突时 EF Core 抛出并发异常，应用决定重试、合并还是返回 409。

**English answer:** Optimistic concurrency avoids holding long database locks. A concurrency token is checked during update, and if another writer changed the row, EF Core reports a conflict. The application then retries, merges, or returns an appropriate conflict response.

## 88. `DbContext` 为什么不是线程安全的？【P0】

**中文解释：** 它内部维护查询和实体跟踪状态，不支持多个并行操作共享同一实例。每个异步 EF 操作应及时 await；若确实要并行执行独立查询，应使用独立 context，而不是对同一个 scoped context 调用 `Task.WhenAll`。

**English answer:** DbContext maintains mutable query and tracking state and does not support concurrent operations. I await each EF call before reusing the context. Truly parallel independent work requires separate context instances.

## 89. database index 有什么作用？【P0】

**中文解释：** 索引让数据库更快定位、排序或连接数据，代价是额外存储和写入维护成本。经常用于筛选、连接、排序和唯一约束的列可能需要索引，但索引顺序和选择性很重要，应根据真实查询计划判断。

**English answer:** An index speeds up lookup, joins, and ordering by maintaining an additional data structure. It costs storage and makes writes more expensive. I design indexes around real query patterns and verify them with query plans.

## 90. INNER JOIN 和 LEFT JOIN 有什么区别？【P0】

**中文解释：** INNER JOIN 只返回两边匹配的行；LEFT JOIN 返回左表全部行，没有匹配时右表列为 null。选择取决于是否必须保留没有关联数据的左侧记录。过滤条件放在 `WHERE` 还是 `ON` 也可能改变 LEFT JOIN 结果。

**English answer:** An inner join returns only matching rows from both sides. A left join keeps every left-side row and uses nulls when no right-side match exists. Predicate placement can change outer-join semantics.

## 91. REST 是什么？【P0】

**中文解释：** REST 是围绕资源、统一接口和无状态交互的一组架构约束，不只是“返回 JSON”。API 通常用名词 URL 表示资源，用 HTTP method 表示操作，并利用状态码、header 和缓存语义。实际设计应优先一致、可预测。

**English answer:** REST is an architectural style centered on resources, a uniform interface, and stateless interactions. HTTP methods, status codes, headers, and caching semantics all contribute; returning JSON alone does not make an API RESTful.

## 92. GET、POST、PUT、PATCH、DELETE 的语义是什么？【P0】

**中文解释：** GET 读取；POST 通常创建资源或触发非幂等操作；PUT 通常完整替换指定资源；PATCH 部分修改；DELETE 删除。语义要体现在 endpoint 行为中，而不只是路由上的特性名称。

**English answer:** GET retrieves, POST commonly creates or triggers a non-idempotent operation, PUT replaces a resource representation, PATCH applies a partial change, and DELETE removes. The implementation should honor those semantics.

## 93. PUT 和 PATCH 有什么区别？【P0】

**中文解释：** PUT 通常提交资源的完整新表示并替换目标，重复相同请求应得到相同结果；PATCH 只描述部分修改。PATCH 需要明确定义缺失字段和显式 null 的区别，并做好字段级验证与权限控制。

**English answer:** PUT normally replaces the full resource representation and is idempotent. PATCH describes a partial update. Patch contracts must distinguish an omitted field from an explicitly null value and validate which fields may change.

## 94. 什么是 idempotency？【P0】

**中文解释：** 幂等表示同一个请求执行一次或多次，对服务器目标资源产生的最终效果相同。GET、PUT、DELETE 按语义应幂等，POST 通常不是。支付或创建操作可用 idempotency key 防止客户端重试造成重复处理。

**English answer:** An idempotent operation has the same intended final effect whether it is performed once or multiple times. GET, PUT, and DELETE are defined as idempotent, while POST usually is not. Idempotency keys can protect retryable creation workflows.

## 95. 常见 HTTP 状态码怎么选？【P0】

**中文解释：** 200 成功返回；201 创建；204 成功但无 body；400 输入无效；401 未认证；403 无权限；404 不存在；409 状态冲突；500 未预期服务端错误。状态码应描述 HTTP 结果，不要所有失败都返回 200 再把错误藏在 JSON 中。

**English answer:** I use status codes to represent the HTTP outcome: 200 for success, 201 for creation, 204 for no content, 400 for invalid input, 401/403 for security, 404 for missing resources, 409 for conflicts, and 500 for unexpected failures.

## 96. pagination 为什么要稳定排序？【P0】

**中文解释：** 没有确定排序时，数据库不保证返回顺序，分页可能重复或漏数据。排序字段不唯一时应追加唯一键作为 tie-breaker，例如 `OrderBy(CreatedAt).ThenBy(Id)`。数据频繁变化或页数很深时可考虑 keyset pagination。

**English answer:** Pagination requires deterministic ordering; otherwise rows can move unpredictably between pages. I add a unique tie-breaker to non-unique sort columns. For deep or frequently changing datasets, keyset pagination may be more stable and efficient.

## 97. caching 有哪些常见层次？【P1】

**中文解释：** 可包含浏览器/CDN 的 HTTP 缓存、应用进程内 memory cache、Redis 等分布式缓存，以及数据库自身缓存。缓存设计的难点是 key、过期、一致性和失效，而不是调用一次 `Get`。多实例应用不能假设每个实例的内存缓存相同。

**English answer:** Caching can exist in HTTP clients or CDNs, application memory, distributed systems such as Redis, and the database. The hard parts are keys, expiration, consistency, and invalidation. In-memory cache is local to one application instance.

## 98. rate limiting 有什么作用？【P1】

**中文解释：** 限流控制客户端在时间窗口内的请求量，保护容量、公平性和成本，也能减轻部分滥用。它不是完整的 DDoS 防护，策略可按 IP、用户、API key 或 endpoint 划分，并应返回明确的 429 和重试信息。

**English answer:** Rate limiting controls request volume to protect capacity, fairness, and cost. Policies can be partitioned by user, API key, IP, or endpoint. It is one protection layer, not a complete DDoS solution.

## 99. Docker 中容器访问数据库为什么不能写 localhost？【P0】

**中文解释：** 容器内的 `localhost` 指当前容器本身，不是宿主机或另一个数据库容器。在 Docker Compose 网络中，服务之间通常用 service name 作为 DNS 主机名，例如 `Host=postgres`。宿主机访问容器则使用映射端口。

**English answer:** Inside a container, localhost refers to that same container. Compose services normally reach each other through the service name on the shared network, such as postgres. Port mappings are mainly for access from the host.

## 100. 如何向面试官介绍一个 .NET Web API 项目？【P0】

**中文解释：** 用 1～2 分钟说明：解决的问题、你的职责、请求从路由到数据库的流程、关键技术选择、一个真实难点和验证结果。不要只罗列 C#、EF、Docker 等名词；要说清为什么这样设计，以及如果流量或需求增长下一步怎么改。

**English answer:** I start with the problem and my responsibility, then walk through one request from routing and validation to business logic and persistence. I explain one meaningful design tradeoff, one challenge I solved, and how I tested the result. I focus on decisions and outcomes rather than listing technologies.

