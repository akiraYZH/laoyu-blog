# .NET Web API 面试 100 题

这套题面向从前端转向 **junior / intermediate .NET Web API** 岗位的开发者。目标不是背诵孤立名词，而是做到：

1. 先用中文解释清楚；
2. 能结合 Web API 场景举例；
3. 能用 20～40 秒英文回答；
4. 面试官追问时，知道边界和常见误区。

## 优先级

- **P0 必会**：高频核心题，应能脱稿回答。
- **P1 重要**：常见追问，应理解并能举例。
- **P2 加分**：掌握后能拉开差距，不必一开始深挖源码。

## 文件目录

| 题号 | 主题 | 文件 |
| --- | --- | --- |
| 01～30 | C# 语言基础 | [01-csharp.md](./01-csharp.md) |
| 31～45 | .NET 运行时与异步 | [02-dotnet-runtime-async.md](./02-dotnet-runtime-async.md) |
| 46～75 | ASP.NET Core Web API | [03-aspnet-core.md](./03-aspnet-core.md) |
| 76～100 | EF Core、SQL、HTTP 与工程实践 | [04-ef-core-sql-api.md](./04-ef-core-sql-api.md) |

## 10 天复习法

每天学习 10 题。第一遍只看中文理解；第二遍遮住答案口述；第三遍只看题目，用英文回答。

| 天数 | 题号 | 复习重点 |
| --- | --- | --- |
| Day 1 | 01～10 | 类型、面向对象、接口 |
| Day 2 | 11～20 | 集合、泛型、委托、LINQ |
| Day 3 | 21～30 | 异常、资源、现代 C# |
| Day 4 | 31～40 | CLR、GC、Task、async/await |
| Day 5 | 41～50 | 并发、程序集、请求管道、DI |
| Day 6 | 51～60 | Controller、路由、绑定、返回值 |
| Day 7 | 61～70 | 验证、认证、授权、配置、日志 |
| Day 8 | 71～80 | API 中间件、测试、DbContext、查询 |
| Day 9 | 81～90 | EF Core 性能、关系、事务、SQL |
| Day 10 | 91～100 | REST、状态码、安全、缓存、Docker |

## 建议的口述结构

回答技术题时可以用一个稳定结构：

> **Definition → Difference → Use case → Pitfall**  
> 先定义，再说区别，然后给使用场景，最后补一个常见坑。

例如回答 `Scoped`：先解释“一次 HTTP 请求一个实例”，再与 Transient/Singleton 比较，然后举 `DbContext` 的例子，最后说明不要把 Scoped 服务直接注入 Singleton。

## 资料口径

内容按当前 Microsoft 官方 `.NET`、`ASP.NET Core` 和 `EF Core` 文档核对。面试回答刻意保持简洁；真实项目中的选择仍要结合版本、负载、安全要求和团队约定。

- [.NET 官方文档](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core 官方文档](https://learn.microsoft.com/aspnet/core/)
- [EF Core 官方文档](https://learn.microsoft.com/ef/core/)
