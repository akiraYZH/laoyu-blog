# EF Core 与 SQL（R056～R075）

## R056. 为什么 DbContext 通常注册为 Scoped？
**口述：** 一次请求对应一次 unit of work：查询、跟踪、保存、释放；它非线程安全，不应跨请求共享。  
**追问/失分点：** Scoped 不等于事务；不要注册 Singleton。  
**英文口述：** A scoped DbContext fits one request-sized unit of work: query, track changes, save, and dispose. It is not thread-safe and must not be shared across requests.  
**英文追问回答：** Scoped lifetime does not automatically create a database transaction, and registering DbContext as singleton is unsafe.

## R057. `AsNoTracking` 什么时候用？
**口述：** 只读查询可减少跟踪开销；之后修改该实体不会自动保存，除非重新 attach。  
**追问/失分点：** 不要对 read-modify-write 机械添加。  
**英文口述：** I use `AsNoTracking` for read-only queries to reduce tracking overhead. Changes to those returned entities are not automatically persisted unless they are attached again.  
**英文追问回答：** I do not add it mechanically to a read-modify-write workflow because that workflow needs tracking or an explicit update strategy.

## R058. 这段列表接口为什么内存越来越高？
**口述：** 检查是否加载完整实体、长期 context 跟踪大量对象、缺少分页或过早 ToList；用投影、no-tracking 和分页缩小结果。  
**追问/失分点：** 用指标证明，不只说“GC 问题”。  
**英文口述：** I check whether the endpoint loads full entities, tracks too many objects in a long-lived context, lacks pagination, or materializes too early. Projection, no-tracking, and bounded pagination reduce the working set.  
**英文追问回答：** I confirm the cause with allocation and query metrics instead of calling every memory increase a GC problem.

## R059. `Include` 为什么可能让查询更慢？
**口述：** 多个集合 Include 可产生行数放大和重复数据；只加载所需关系，优先 DTO projection，必要时评估 split query。  
**追问/失分点：** Include 不是避免 N+1 的唯一答案。  
**英文口述：** Multiple collection `Include` operations can multiply rows and duplicate data in the result. I load only required relationships, prefer DTO projection, and evaluate split queries when appropriate.  
**英文追问回答：** Include is only one N+1 solution; projection or a separate bounded query may produce a better shape.

## R060. 什么代码容易产生 N+1？
**口述：** 先查父集合，再在循环或 lazy loading 中逐个查子集合；用 SQL 日志确认，改成投影、Include 或批量查询。  
**追问/失分点：** 说清 1+N 次往返而非只背名词。  
**英文口述：** N+1 occurs when one query loads parents and a loop or lazy-loading access runs one query per parent. I confirm it in SQL logs and replace it with projection, eager loading, or batching.  
**英文追问回答：** I describe the actual one-plus-N database round trips rather than merely naming the pattern.

## R061. 为什么先 `ToListAsync` 再 `Where` 不好？
**口述：** 它先把更多行拉入内存，数据库无法利用筛选减少传输；应在 IQueryable 上组合后再物化。  
**追问/失分点：** 哪些本地方法不能翻译？  
**英文口述：** Calling `ToListAsync` first transfers more rows and makes later `Where` run in memory. I compose supported filtering on `IQueryable` before materialization.  
**英文追问回答：** A local method that the provider cannot translate must be redesigned, translated explicitly, or applied after a deliberately bounded materialization.

## R062. Projection 为什么比返回 Entity 更适合 API？
**口述：** `Select` DTO 只取需要列，降低传输、跟踪和序列化成本，也避免内部字段泄露。  
**追问/失分点：** 投影要在 ToList 前。  
**英文口述：** A DTO projection selects only required columns in SQL, reducing transfer, tracking, serialization, and accidental field exposure. I project before `ToListAsync`.  
**英文追问回答：** Mapping after materialization cannot help the database avoid reading the unnecessary columns.

## R063. `SaveChanges` 是否自动使用事务？
**口述：** 支持事务的 provider 通常保证单次 SaveChanges 原子性；跨多次保存或混合数据操作才考虑显式事务。  
**追问/失分点：** 显式事务与 retry strategy 的关系。  
**英文口述：** For a transactional provider, one `SaveChanges` normally applies its changes atomically. I use an explicit transaction when one business operation spans multiple saves or data-access mechanisms.  
**英文追问回答：** Explicit transactions must be coordinated with the provider's retry execution strategy rather than combined blindly.

## R064. 唯一 slug 冲突为什么不能只先查询再插入？
**口述：** 两个请求可同时通过检查，存在 race condition；数据库 unique constraint 才是保证，应用捕获 provider 异常映射 409。  
**追问/失分点：** 检查仍可用于友好提示但不能保证。  
**英文口述：** A pre-check for a slug races because two requests can both pass it. A database unique constraint guarantees correctness, and the API translates the known violation to 409.  
**英文追问回答：** The pre-check may improve the message but never replaces the constraint under concurrency.

## R065. 两个人同时编辑文章如何避免覆盖？
**口述：** 使用 row version/concurrency token 做 optimistic concurrency；更新影响零行时捕获冲突，选择重试、合并或返回 409。  
**追问/失分点：** 不要声称 EF 自动知道业务合并规则。  
**英文口述：** I use a row version or concurrency token for optimistic concurrency. A stale update raises a concurrency conflict, and the application chooses whether to retry, merge, or return 409.  
**英文追问回答：** EF can detect the conflict but cannot decide the domain-specific merge policy automatically.

## R066. Migration 生成后能直接上生产吗？
**口述：** 要审查 SQL、锁表、数据丢失和回填影响；生产用受控脚本或发布阶段执行并准备备份与回滚。  
**追问/失分点：** 新增 required 列给旧数据怎么办？  
**英文口述：** I review generated migration SQL for locking, data loss, and backfill cost. Production migrations run through a controlled deployment step with backup and rollback planning.  
**英文追问回答：** Adding a required column to existing rows needs a staged migration rather than a blind one-step change.

## R067. 新增非空列且表里已有数据，怎么迁移？
**口述：** 分阶段：先 nullable/临时默认值，部署并回填，再验证无 null，最后改为 required；避免一次破坏性迁移。  
**追问/失分点：** 大表回填怎样分批？  
**英文口述：** I first add the column as nullable or with a temporary safe default, deploy compatible code, backfill and verify the data, and only then enforce the non-null constraint.  
**英文追问回答：** For a large table, I backfill in bounded batches to control locks, logs, and transaction duration.

## R068. Offset pagination 为什么会重复或漏数据？
**口述：** 数据在翻页间插入删除会改变 offset；必须稳定排序，频繁变化或深页使用 keyset/cursor。  
**追问/失分点：** 非唯一排序追加 Id。  
**英文口述：** Offset pages can shift when rows are inserted or deleted between requests. I apply deterministic ordering and use keyset pagination for deep or frequently changing data.  
**英文追问回答：** If the main sort column is not unique, I append a unique key such as Id as a tie-breaker.

## R069. 查询有索引为什么仍可能慢？
**口述：** 可能选择性差、复合索引顺序错误、函数阻止使用、返回行太多或统计信息问题；查看真实 query plan。  
**追问/失分点：** 索引会增加写入成本。  
**英文口述：** An index can still be ineffective because of low selectivity, wrong composite-column order, a function on the indexed column, excessive returned rows, or poor statistics. I inspect the actual query plan.  
**英文追问回答：** Every index also consumes storage and adds write-maintenance cost, so more indexes are not automatically better.

## R070. INNER JOIN 与 LEFT JOIN 的业务区别？
**口述：** INNER 只保留匹配项，LEFT 保留所有左侧记录；WHERE 中过滤右表可能意外把 LEFT 变成类似 INNER。  
**追问/失分点：** 用“没有评论的文章”举例。  
**英文口述：** An inner join returns only matching rows, while a left join preserves all left-side rows and uses nulls for missing matches. Predicate placement can change left-join semantics.  
**英文追问回答：** For example, a left join keeps articles with no comments, but filtering right-side columns in WHERE may remove those articles.

## R071. 什么时候会出现 deadlock？
**口述：** 多事务以不同顺序持锁并等待对方；统一访问顺序、缩短事务、加合适索引，并对可重试失败做有限重试。  
**追问/失分点：** 应用 lock 不能解决数据库 deadlock。  
**英文口述：** Database deadlocks occur when transactions acquire locks in conflicting orders and wait for each other. I keep transactions short, use consistent access order and useful indexes, and retry only transient failures with limits.  
**英文追问回答：** An in-process C# lock cannot resolve a deadlock among database transactions or application instances.

## R072. Repository pattern 是否必须包一层 EF Core？
**口述：** 不必须；DbContext 已提供 unit-of-work/repository 能力。只有领域边界、复用查询或替换需求明确时才加，避免只转发每个方法。  
**追问/失分点：** 测试不等于必须 mock repository。  
**英文口述：** I do not automatically wrap EF Core in a forwarding repository because DbContext already provides repository and unit-of-work behavior. I add a repository only for a meaningful domain or reusable-query boundary.  
**英文追问回答：** Testability alone does not require mocking every query; provider behavior is often more reliable in integration tests.

## R073. 如何查看 EF Core 实际执行的 SQL？
**口述：** 开发中用日志或 `ToQueryString`，线上用安全采样和 tracing；关注次数、参数、耗时和执行计划。  
**追问/失分点：** 不要在生产记录敏感参数。  
**英文口述：** During development I inspect generated SQL through logging or `ToQueryString`; in production I use safe sampling and tracing. I examine query count, parameters, duration, and execution plans.  
**英文追问回答：** Sensitive parameter logging must remain disabled or carefully controlled in production.

## R074. `SaveChangesAsync` 失败后还能继续用同一 Context 吗？
**口述：** 取决于失败类型；某些异常后跟踪状态仍含失败实体，需修正、清理或丢弃 context。Context 本来就应短生命周期。  
**追问/失分点：** EF 的 InvalidOperationException 可能表示不可恢复状态。  
**英文口述：** Whether a context is reusable after `SaveChangesAsync` fails depends on the failure. Because tracking state may still contain failed entities, I repair and clear it deliberately or discard the short-lived context.  
**英文追问回答：** Some EF `InvalidOperationException` cases indicate an unrecoverable context state, so continuing blindly is unsafe.

## R075. 数据库慢，你如何区分应用问题与 SQL 问题？
**口述：** 用 trace 对齐请求与 SQL，检查连接池等待、查询次数、单条耗时、锁和计划，再做针对性修改并压测验证。  
**追问/失分点：** 不要先加缓存掩盖根因。  
**英文口述：** I correlate request traces with database spans and separate connection-pool waits, query count, individual SQL duration, locks, and execution plans. I then change one cause and load-test the result.  
**英文追问回答：** Adding cache before finding the bottleneck may only hide stale or inefficient database behavior.

