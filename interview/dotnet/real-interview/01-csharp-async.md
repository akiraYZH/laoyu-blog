# C# 与异步（R001～R025）

## R001. `var b = a` 后修改 b，为什么 a 也变了？
**口述：** class 是引用类型，赋值复制引用，二者指向同一实例；需要独立对象时应明确复制字段。  
**追问：** struct、record、浅复制分别会怎样？  
**失分点：** 用“class 在堆、struct 在栈”定义两者。  
**英文口述：** `User` is a class, so assignment copies its reference. Both variables point to the same instance; an independent object requires explicit copying.  
**英文追问回答：** A struct is copied by value; a record is not automatically immutable; a shallow copy still shares nested references.

## R002. 你在项目里什么时候用 interface，什么时候用 abstract class？
**口述：** 可替换能力或测试边界用接口；确实需要共享状态和基类实现才用抽象类，不为每个类机械加接口。  
**追问：** 为什么 DTO 不需要接口？一个实现的 service 要不要接口？  
**失分点：** 只背“接口多继承、抽象类单继承”。  
**英文口述：** I use an interface for a replaceable capability or testing boundary, and an abstract class when implementations genuinely share state or base behavior. I do not create interfaces mechanically.  
**英文追问回答：** DTOs normally need no behavioral interface, and a one-implementation service needs one only if it creates a useful boundary.

## R003. 为什么 DTO 常用 record，Entity 常用 class？
**口述：** DTO 偏值语义，record 的值相等和复制方便；Entity 通常有身份和可变生命周期，更适合 class，但不是硬规则。  
**追问：** record 是否自动不可变？请求和响应 DTO 要不要分开？  
**失分点：** 认为 record 一定 immutable。  
**英文口述：** DTOs are value-like, so record equality and copying are useful. Entities usually have identity and a mutable lifecycle, so classes often fit better; this is not a strict rule.  
**英文追问回答：** Records can be mutable, and request and response DTOs should differ when their allowed fields differ.

## R004. `IEnumerable` 与 `IQueryable` 会怎样影响 EF 查询？
**口述：** IQueryable 保存表达式供 EF 翻译成 SQL；物化后 IEnumerable 操作在内存执行，过滤分页应尽量放在 `ToListAsync` 前。  
**追问：** `AsEnumerable` 后会怎样？为什么不随意暴露 IQueryable？  
**失分点：** 简化成“一个内存、一个数据库”。  
**英文口述：** `IQueryable` stores expressions that EF Core can translate to SQL; after materialization, later `IEnumerable` operations run in memory. I filter and paginate before `ToListAsync` without exposing unrestricted queries.  
**英文追问回答：** `AsEnumerable` switches later work to LINQ-to-Objects; local methods may not translate, and leaked queries are hard to control.

## R005. LINQ 查询什么时候真正执行？
**口述：** 多数操作延迟执行，枚举、`ToList`、`First` 等才触发；重复枚举数据库 query 可能重复发 SQL。  
**追问：** 数据源中途变化会怎样？什么时候应物化？  
**失分点：** 认为调用 `Where` 就生成结果。  
**英文口述：** Most LINQ queries use deferred execution and run when enumerated or materialized. Source changes before enumeration may affect the result.  
**英文追问回答：** Re-enumerating an EF query can execute SQL again; materialize when a stable snapshot is required.

## R006. 唯一 slug 查询用 `FirstOrDefault` 还是 `SingleOrDefault`？
**口述：** 多条记录代表数据错误时用 Single 暴露问题，但真正唯一性必须由数据库 unique constraint 保证。  
**追问：** 没有唯一索引会怎样？找不到如何映射 404？  
**失分点：** 认为 Single 能建立唯一约束。  
**英文口述：** I use `SingleOrDefault` when duplicates represent corrupted data, but only a database unique constraint guarantees uniqueness under concurrency.  
**英文追问回答：** Without the constraint concurrent inserts can duplicate a slug; no result maps to 404, while duplicates are an integrity error.

## R007. 为什么循环拼接 string 可能慢？
**口述：** string 不可变，反复拼接会产生临时对象；大量构造用 StringBuilder，少量插值保持简单并先测量。  
**追问：** string 是引用类型吗？何时无需优化？  
**失分点：** 宣称 StringBuilder 永远更快。  
**英文口述：** Strings are immutable, so large repeated concatenation can allocate temporary objects. I use `StringBuilder` for substantial loops but keep simple interpolation unless measurement shows a problem.  
**英文追问回答：** String is a reference type, and a few concatenations normally need no optimization.

## R008. 你会在哪里捕获异常？
**口述：** 只在能恢复、补充上下文或转换错误时捕获；未知异常交给全局处理，不能吞异常或泄露 stack trace。  
**追问：** `throw` 与 `throw ex`？409 何时使用？  
**失分点：** 每层 catch、记录、再抛，造成重复日志。  
**英文口述：** I catch exceptions only where I can recover, add context, or translate them. Unexpected API failures are handled centrally without exposing stack traces.  
**英文追问回答：** Bare `throw` preserves the stack; a state conflict can map to 409; logging the same exception at every layer creates duplicates.

## R009. `using` 和 GC 分别解决什么？
**口述：** using 确定性调用 Dispose 释放连接、流等资源；GC 在不确定时间回收不可达托管内存。  
**追问：** `await using`？DI 创建的 disposable 谁释放？  
**失分点：** 认为 Dispose 会立即释放对象内存。  
**英文口述：** `using` calls `Dispose` deterministically for streams or connections, while the GC later reclaims unreachable managed memory. Dispose does not directly free managed memory.  
**英文追问回答：** `await using` supports async disposal, and the DI container disposes the disposable services it creates.

## R010. event 为什么可能导致内存泄漏？
**口述：** 长生命周期发布者持有订阅者 delegate，订阅者仍可达而不能被 GC；必要时按生命周期退订。  
**追问：** 静态事件风险？匿名 lambda 如何退订？  
**失分点：** 把所有内存增长都归咎于 GC。  
**英文口述：** A publisher holds delegates that reference subscribers. A long-lived publisher can keep an unused subscriber reachable, so subscription lifetime must be managed.  
**英文追问回答：** Static events may live for the process; unsubscribing a lambda requires keeping the original delegate instance.

## R011. `async` 是否会创建新线程？
**口述：** 不一定；async/await 是状态机和 continuation，I/O 等待通常不占线程，CPU 工作才可能使用线程池。  
**追问：** Task 与 Thread？async 为什么提高服务器吞吐？  
**失分点：** 说“一个 async 一个线程”。  
**英文口述：** `async` does not automatically create a thread. It builds a state machine, and incomplete I/O can yield without occupying a request thread; CPU work is a separate thread-pool decision.  
**英文追问回答：** Task represents an operation and Thread an execution resource; async I/O improves throughput, not necessarily single-request speed.

## R012. 为什么 Web API 不建议 `.Result` 或 `.Wait()`？
**口述：** 它们阻塞请求线程、降低吞吐，并可能带来死锁或异常包装；应让 async 沿调用链传播。  
**追问：** ASP.NET Core 没传统 SynchronizationContext，为何仍有问题？  
**失分点：** 只会回答“必然死锁”。  
**英文口述：** `.Result` and `.Wait()` block request threads, reduce throughput, and can complicate exceptions or deadlock in some contexts. I keep the call chain async and use `await`.  
**英文追问回答：** ASP.NET Core deadlocks are less common without the classic SynchronizationContext, but blocking still causes starvation and poor scalability.

## R013. 什么时候用 `Task.WhenAll`？
**口述：** 独立 I/O 可先启动再共同等待；要限制并发、传递取消并处理失败，不能并行使用同一 DbContext。  
**追问：** 一个任务失败会怎样？如何取得全部异常？  
**失分点：** 对无限集合直接 WhenAll。  
**英文口述：** I use `WhenAll` for bounded independent I/O, while propagating cancellation and handling failures. I never run parallel operations on one `DbContext`.  
**英文追问回答：** The combined task fails if a child fails; its `Exception` contains all faults, and dependent operations should remain sequential.

## R014. 客户端断开后查询为什么还在跑？
**口述：** 请求 CancellationToken 没有沿 Controller、service 传到 EF 或 HttpClient；取消是协作式的。  
**追问：** 写操作取消如何保证一致性？超时与取消区别？  
**失分点：** 新建一个与请求无关的 token。  
**英文口述：** The query may continue because the request token was not passed through the controller and service to EF or `HttpClient`. Cancellation is cooperative and must be observed throughout the chain.  
**英文追问回答：** Writes must preserve transaction consistency; timeout is a time policy, while cancellation can come from disconnect or shutdown.

## R015. `async void` 为什么危险？
**口述：** 调用方无法 await、组合或正常观察异常；除框架 event handler 外应返回 Task。  
**追问：** 可靠后台任务怎么做？漏掉 await 会怎样？  
**失分点：** 用 async void 做数据库写入。  
**英文口述：** `async void` cannot be awaited or composed, and its exceptions do not flow through a Task. Except for event handlers, async methods should return `Task` or `Task<T>`.  
**英文追问回答：** Reliable background work belongs in a hosted service or queue; a missing await can hide failure and finish the request too early.

## R016. 为什么同一个 DbContext 不能并行查询？
**口述：** DbContext 有可变跟踪状态且非线程安全；应立即 await，真正独立并行工作使用独立 context。  
**追问：** `IDbContextFactory` 何时使用？  
**失分点：** 用 lock 包住同一 context 作为解决方案。  
**英文口述：** `DbContext` has mutable tracking state and is not thread-safe. I await each operation; genuinely parallel work needs separate contexts.  
**英文追问回答：** `IDbContextFactory` helps when a request scope does not match the unit of work, such as background or parallel independent jobs.

## R017. Transient 是否线程安全，Singleton 是否不安全？
**口述：** 生命周期不等于线程安全；Transient 也可能访问共享静态状态，Singleton 若不可变或正确同步可以安全。  
**追问：** Scoped 服务能否在请求内被并发调用？  
**失分点：** 仅凭注册方式判断线程安全。  
**英文口述：** Lifetime does not determine thread safety. A transient may touch static state, while an immutable or synchronized singleton may be safe; actual shared mutable state matters.  
**英文追问回答：** A scoped service may still be called concurrently by parallel tasks within one request.

## R018. `lock` 和 `SemaphoreSlim` 怎么选？
**口述：** lock 用于短同步临界区且不能 await；SemaphoreSlim 支持异步等待和限制并发，两者都不是分布式锁。  
**追问：** 为什么 Release 放 finally？跨实例怎么办？  
**失分点：** await 时持有普通 lock。  
**英文口述：** I use `lock` for short synchronous critical sections and `SemaphoreSlim` for asynchronous waiting or bounded concurrency. Neither is a distributed lock.  
**英文追问回答：** Release belongs in `finally`; cross-instance coordination needs a database or distributed mechanism.

## R019. 返回 null、Result 还是抛异常？
**口述：** 预期的不存在可用 nullable/Try；正常业务失败用明确 Result；不可恢复的异常才 throw。  
**追问：** unique conflict 如何返回 409？repository Find 返回 null 合理吗？  
**失分点：** 用异常控制所有普通分支。  
**英文口述：** I use nullable or Try for expected absence, an explicit Result for business failures, and exceptions for exceptional conditions. The contract must preserve the reason.  
**英文追问回答：** Repository `Find` may return null; a unique conflict can become a Result that the API maps to 409.

## R020. 如何测试 async 方法？
**口述：** 测试本身返回 Task 并 await，覆盖成功、异常和取消，不用 Thread.Sleep；EF 翻译行为用集成测试。  
**追问：** 怎样断言异步异常？为什么 mock DbSet 容易失真？  
**失分点：** 测试启动任务但不 await。  
**英文口述：** The test returns Task and awaits the method, covering success, failure, and cancellation without sleeps. Provider-specific EF queries belong in integration tests.  
**英文追问回答：** Async exception assertions must await the delegate; trigger a real token for cancellation; mocked DbSet translation may differ from the provider.

## R021. `List`、`HashSet`、`Dictionary` 在 API 中怎么选？
**口述：** 顺序列表用 List，唯一成员检测用 HashSet，按键查值用 Dictionary；依据访问模式而不是习惯。  
**追问：** 平均复杂度？自定义 key 为什么要正确实现 equality？  
**失分点：** 只背复杂度、不考虑顺序和重复语义。  
**英文口述：** I choose `List` for ordered data, `HashSet` for uniqueness and membership, and `Dictionary` for key lookup. The access pattern drives the choice.  
**英文追问回答：** Hash lookup is average O(1), and custom keys need stable, consistent equality and hash codes.

## R022. `Equals` 与 `==` 不一致会造成什么问题？
**口述：** 值对象比较、HashSet 和 Dictionary 行为可能不一致；重写 Equals 时必须一致实现 GetHashCode。  
**追问：** 可变对象作为 Dictionary key 有何风险？  
**失分点：** 只重写 Equals，不重写 GetHashCode。  
**英文口述：** Inconsistent `Equals`, `==`, and `GetHashCode` break value comparisons and hashed collections, so I implement them consistently.  
**英文追问回答：** Mutating a key field after insertion can change its hash and make the dictionary entry unreachable.

## R023. closure 在循环中可能带来什么问题？
**口述：** lambda 捕获变量而不是当时的值，延迟执行时可能看到最终状态；应确认循环变量作用域或复制局部值。  
**追问：** closure 如何延长对象生命周期？  
**失分点：** 看到 lambda 就认为一定有 bug。  
**英文口述：** A closure captures a variable rather than a frozen value, so delayed execution may observe a later value and captured references may live longer.  
**英文追问回答：** Create a local per-iteration copy when needed and avoid capturing unnecessarily large or long-lived objects.

## R024. 什么时候用 `Span<T>`？普通 Web API 必须学吗？
**口述：** Span 适合性能敏感的连续内存切片并减少分配，但受 ref struct 生命周期限制；普通 CRUD 先保持清晰并通过 profiling 证明需求。  
**追问：** 为什么不能跨 await？  
**失分点：** 为面试炫技而把业务代码全部改成 Span。  
**英文口述：** `Span<T>` is a low-allocation view over contiguous memory for measured performance-sensitive code. Ordinary CRUD should stay clear until profiling proves a need.  
**英文追问回答：** Span is a `ref struct`, so its lifetime restrictions prevent it from crossing an `await` boundary.

## R025. 你如何解释依赖倒置，而不是只背 SOLID？
**口述：** 高层业务不直接控制低层细节，而依赖稳定契约，由启动代码注入实现；目的是降低变化影响，不是增加层数。  
**追问：** 何时直接依赖具体类反而合理？  
**失分点：** 每个类都建立一一对应接口。  
**英文口述：** High-level policy depends on a stable contract, while the composition root supplies the low-level implementation. The goal is to contain change, not add layers.  
**英文追问回答：** A concrete dependency is reasonable when it is simple, stable, and no useful replacement or test boundary exists.

