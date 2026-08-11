# 第二部分：.NET 运行时与异步（31～45）

## 31. .NET、CLR 和 C# 是什么关系？【P0】

**中文解释：** C# 是编程语言；.NET 是包含运行时、基础类库、SDK 和应用框架的平台；CLR 是执行托管代码的运行时，负责 JIT、GC、异常、类型安全等。C# 代码通常先编译成 IL，再由 CLR 执行。

**English answer:** C# is a programming language, .NET is the development platform, and the CLR is the runtime that executes managed code. C# normally compiles to intermediate language, which the CLR loads and executes.

## 32. managed code 和 unmanaged code 有什么区别？【P1】

**中文解释：** 托管代码在 CLR 管理下执行，可使用 GC、类型安全和统一异常机制；非托管代码直接以本机代码运行，资源和内存管理通常由程序自身负责。P/Invoke 和 native interop 可以连接两者，但要特别处理资源生命周期和数据封送。

**English answer:** Managed code runs under the CLR and benefits from services such as garbage collection and type safety. Unmanaged code runs directly as native code and usually manages its own resources. Interop connects them but adds lifetime and marshaling concerns.

## 33. IL 和 JIT 是什么？【P0】

**中文解释：** C# 编译器通常把源码编译成中间语言 IL 和元数据。JIT 在运行时把实际需要执行的方法编译成本机机器码，使同一程序集可在不同平台由对应运行时执行。现代 .NET 还支持 ReadyToRun 和 Native AOT 等提前编译方案。

**English answer:** The C# compiler usually produces intermediate language and metadata. The JIT compiler turns methods into native machine code when they are needed. .NET also offers ahead-of-time options for different startup and deployment tradeoffs.

## 34. CTS 和 CLS 是什么？【P2】

**中文解释：** CTS 定义 .NET 中类型如何声明、使用和管理，让不同 .NET 语言共享类型系统；CLS 是 CTS 的一组公共规则，公共 API 遵循它可以提高跨语言兼容性。CLS 不是完整类型系统，而是面向语言互操作的子集。

**English answer:** The Common Type System defines how types work across .NET languages. The Common Language Specification is a compatible subset that public APIs can follow for better language interoperability.

## 35. assembly、DLL 和 namespace 有什么区别？【P1】

**中文解释：** assembly 是 .NET 的部署和版本单元，包含 IL、元数据和 manifest，常见文件形式是 DLL 或 EXE；namespace 只是组织类型名称、防止命名冲突的逻辑分组。一个程序集可包含多个 namespace，同一 namespace 也可跨多个程序集。

**English answer:** An assembly is a deployment and versioning unit containing code and metadata, usually a DLL or EXE. A namespace is only a logical naming organization. They do not have a one-to-one relationship.

## 36. GC 是怎么工作的？【P0】

**中文解释：** GC 自动跟踪从根对象仍可到达的托管对象，并回收不可达对象的内存，必要时还会压缩堆。它减少手动内存管理错误，但回收时间不确定，也不会自动及时释放数据库连接、文件句柄等外部资源。

**English answer:** The garbage collector finds managed objects that are no longer reachable from roots and reclaims their memory. It may also compact the heap. Collection timing is nondeterministic, so external resources still need deterministic cleanup.

## 37. GC 的 Generation 0、1、2 是什么？【P1】

**中文解释：** .NET 基于“大多数对象很快死亡”的假设按代管理对象。新对象通常在 Gen 0，存活后晋升到 Gen 1、Gen 2；低代收集通常更频繁、更便宜。大对象通常进入 LOH，频繁创建大对象也会造成压力。

**English answer:** The GC groups objects into generations based on age. New objects start in generation 0 and survivors may be promoted to generations 1 and 2. This makes collecting short-lived objects cheaper, while long-lived and large objects need special attention.

## 38. finalizer 和 Dispose 有什么区别？【P1】

**中文解释：** `Dispose` 由代码确定性调用，适合及时释放资源；finalizer 由 GC 在不确定时间调用，是直接持有非托管资源时的最后保障，但成本更高。常见包装类型通过 `SafeHandle` 管理非托管句柄，使用者仍应 `using`。

**English answer:** Dispose is called deterministically by application code. A finalizer is a nondeterministic safety net run by the GC for objects that directly own unmanaged resources. SafeHandle is generally preferred for wrapping native handles.

## 39. `async/await` 是怎么工作的？【P0】

**中文解释：** 编译器把 async 方法转换成状态机。遇到尚未完成的 await 时，方法把控制权返回给调用方，并在操作完成后安排 continuation；它不会因为写了 `async` 就自动创建新线程。对 I/O 操作，等待期间通常不占用工作线程。

**English answer:** The compiler transforms an async method into a state machine. If an awaited operation is incomplete, the method returns control and schedules a continuation for later. Async does not automatically create a new thread and is especially useful for non-blocking I/O.

## 40. Task 和 Thread 有什么区别？【P0】

**中文解释：** Thread 是操作系统调度的执行线程；Task 表示一个可能尚未完成的异步操作，是更高层抽象。Task 可能在线程池运行 CPU 工作，也可能代表没有线程被阻塞的 I/O。Web API 通常用 Task 表达异步，不直接为每个请求创建 Thread。

**English answer:** A thread is an operating-system execution resource. A Task is a higher-level representation of an asynchronous operation and may or may not be using a thread at a given moment. Web APIs normally compose Tasks instead of creating threads per request.

## 41. 为什么不应该用 `.Result` 或 `.Wait()` 阻塞异步代码？【P0】

**中文解释：** 同步阻塞会占住线程，降低 Web 服务器吞吐量；在某些同步上下文中还可能造成死锁。应让异步沿调用链传播并使用 `await`。但也不要为了形式把纯同步、极快的操作包装成 `Task.Run`。

**English answer:** Result and Wait block a thread and reduce scalability; in some synchronization contexts they can also deadlock. I prefer async all the way with await. I do not wrap naturally synchronous work in Task.Run without a reason.

## 42. `Task.WhenAll` 和逐个 `await` 有什么区别？【P0】

**中文解释：** 彼此独立的异步操作先启动后用 `WhenAll` 等待，可以并发等待并减少总耗时；逐个 await 会串行执行。若操作依赖同一个非线程安全资源，例如同一个 `DbContext`，则不能盲目并发。

**English answer:** WhenAll lets independent asynchronous operations make progress concurrently, while awaiting each one before starting the next makes them sequential. I only parallelize when the operations and their dependencies, such as DbContext, are safe to use concurrently.

## 43. `CancellationToken` 有什么用？【P0】

**中文解释：** 它在调用链中传递协作式取消信号，让数据库、HTTP 请求或长任务在客户端断开或超时后尽快停止。取消不是强制终止线程；被调用代码要观察 token，并通常抛出 `OperationCanceledException` 或返回取消状态。

**English answer:** CancellationToken carries a cooperative cancellation request through the call chain. Operations must observe it and stop safely; it does not forcibly kill a thread. In a Web API I pass the request token to database and outbound HTTP calls.

## 44. thread safety 和 race condition 是什么？【P1】

**中文解释：** 多个执行流并发访问共享可变状态时，如果结果依赖不可预测的执行顺序，就会发生竞态。可通过不可变数据、限制共享、`lock`、并发集合或原子操作解决。锁的范围应尽量小，并避免在 `lock` 内 await。

**English answer:** A race condition occurs when concurrent access to shared mutable state produces order-dependent results. I first avoid or isolate shared state, then use synchronization or concurrent collections where necessary. I also avoid awaiting while holding a normal lock.

## 45. 什么是 memory leak？有 GC 为什么仍会泄漏？【P1】

**中文解释：** 在托管环境中，“泄漏”常指不再需要的对象仍可从 GC root 到达，因此无法回收。常见原因包括静态集合无限增长、事件未退订、缓存无上限、计时器或长生命周期闭包持有引用。GC 只能回收不可达对象，不能判断业务上是否还需要。

**English answer:** A managed memory leak happens when objects are no longer useful but remain reachable from GC roots. Static collections, event subscriptions, unbounded caches, and long-lived closures are common causes. The GC understands reachability, not business intent.

