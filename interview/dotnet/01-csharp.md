# 第一部分：C# 语言基础（01～30）

## 01. value type 和 reference type 有什么区别？【P0】

**中文解释：** 值类型变量直接保存值，赋值时复制该值；引用类型变量保存对象引用，赋值时复制引用，所以两个变量可能指向同一对象。`struct`、`enum` 和基本数值类型通常是值类型；`class`、数组、委托和 `string` 是引用类型。注意：值类型不等于一定在栈上，实际位置取决于它所在的上下文。

**English answer:** A value-type variable contains its value, and assignment copies that value. A reference-type variable contains a reference to an object, so assignment copies the reference. The stack-versus-heap explanation is only an implementation detail and is not the definition.

## 02. class 和 struct 应该怎么选？【P0】

**中文解释：** `class` 是引用类型，适合有身份、生命周期和共享可变状态的对象；`struct` 是值类型，适合较小、表示单个值、最好不可变的数据。大而且频繁复制的 struct 可能带来性能成本。不要仅仅为了“放在栈上”而选 struct。

**English answer:** I use a class for entities with identity and shared mutable state. I use a small, preferably immutable struct for value-like data such as coordinates or money values. I do not choose a struct merely because I assume it will live on the stack.

## 03. string 为什么是引用类型却表现得像值？【P0】

**中文解释：** `string` 是引用类型，但它不可变。拼接、替换、转大写等操作不会修改原字符串，而是返回新字符串；相同文本还可能被字符串驻留复用。大量循环拼接通常用 `StringBuilder`，避免产生许多临时字符串。

**English answer:** String is a reference type, but it is immutable. Operations such as Replace or ToUpper return a new string instead of changing the original one. For repeated concatenation in a loop, StringBuilder can reduce temporary allocations.

## 04. `==`、`Equals` 和 `ReferenceEquals` 有什么区别？【P1】

**中文解释：** `ReferenceEquals` 只判断两个引用是否指向同一对象。`Equals` 表示值相等语义，类型可以重写；`==` 是运算符，也可以被重载，因此具体行为由类型决定。自定义值对象时，应一致地实现 `Equals`、`GetHashCode`，必要时再重载 `==`。

**English answer:** ReferenceEquals checks object identity. Equals expresses value equality and can be overridden, while the == operator can be overloaded. For a value object, equality members and GetHashCode should be implemented consistently.

## 05. boxing 和 unboxing 是什么？【P1】

**中文解释：** boxing 是把值类型包装成 `object` 或其实现的接口，通常会产生对象分配并复制值；unboxing 是从包装对象中取回准确的值类型。频繁装箱会增加分配和 GC 压力，泛型集合可避免很多装箱。

**English answer:** Boxing wraps and copies a value type into an object or interface reference. Unboxing extracts the exact value type from that boxed object. Generic collections help avoid the allocation and casting costs of repeated boxing.

## 06. `var`、`dynamic` 和 `object` 有什么区别？【P1】

**中文解释：** `var` 仍是编译期静态类型，只是类型由编译器推断；`object` 是所有 .NET 类型的共同基类，使用具体成员前通常要转换；`dynamic` 把成员检查推迟到运行时，写起来灵活但可能产生运行时错误。

**English answer:** Var is statically typed; the compiler simply infers the type. Object is the common base type and normally requires casting to use specific members. Dynamic defers member binding until runtime, trading compile-time safety for flexibility.

## 07. interface 和 abstract class 有什么区别？【P0】

**中文解释：** 接口主要表达能力或契约，一个类型可以实现多个接口；抽象类可以保存实例状态、构造逻辑和共享实现，但类只能继承一个直接基类。需要跨不同类型定义能力时选接口，需要共享状态和模板实现时考虑抽象类。

**English answer:** An interface defines a contract and supports multiple implementations on one type. An abstract class can hold state, constructors, and shared implementation, but a class has only one base class. I choose based on whether I need a capability contract or shared base behavior.

## 08. `virtual`、`abstract`、`override` 和 `new` 分别是什么？【P0】

**中文解释：** `virtual` 提供默认实现并允许派生类重写；`abstract` 没有实现并强制非抽象派生类实现；`override` 参与运行时多态；`new` 只是隐藏基类成员，调用哪个成员取决于变量的编译期类型，容易造成困惑。

**English answer:** Virtual provides an overridable implementation, while abstract requires derived classes to implement the member. Override participates in runtime polymorphism. New only hides a base member and its behavior depends on the variable's compile-time type.

## 09. 什么是 encapsulation、inheritance 和 polymorphism？【P0】

**中文解释：** 封装把状态和行为放在类型内部，并限制不必要的访问；继承让派生类型复用或扩展基类行为；多态让调用方通过共同抽象操作不同具体类型。实践中优先用封装和组合，只有明确的 “is-a” 关系才使用继承。

**English answer:** Encapsulation protects state behind a clear API. Inheritance derives one type from another, and polymorphism lets callers work through a common abstraction. I prefer composition unless there is a genuine is-a relationship.

## 10. access modifiers 有哪些常见类型？【P1】

**中文解释：** `public` 到处可访问；`private` 只在声明类型内；`protected` 可由声明类型和派生类型访问；`internal` 只在当前程序集；`protected internal` 是两者满足其一；`private protected` 要求同一程序集内的派生类型。

**English answer:** Public is accessible everywhere, private only inside the declaring type, protected in the type and derived types, and internal within the assembly. Protected internal is the union, while private protected is the intersection of derived type and same assembly.

## 11. Array、List、HashSet 和 Dictionary 怎么选？【P0】

**中文解释：** Array 长度固定且连续；`List<T>` 是可动态扩容的顺序集合；`HashSet<T>` 保存唯一元素并提供平均 O(1) 查找；`Dictionary<TKey,TValue>` 按唯一键查值。选择依据是访问方式、是否去重、是否需要键值映射，而不是习惯性全用 List。

**English answer:** I use an array for fixed-size indexed data, List for an ordered growable collection, HashSet for uniqueness and fast membership tests, and Dictionary for key-value lookup. The access pattern should drive the choice.

## 12. `IEnumerable<T>` 和 `ICollection<T>`、`IList<T>` 有什么区别？【P1】

**中文解释：** `IEnumerable<T>` 只保证可以顺序枚举；`ICollection<T>` 增加数量及增删能力；`IList<T>` 再增加按索引访问和插入。方法参数应暴露满足需求的最小接口，减少调用方与具体实现的耦合。

**English answer:** IEnumerable only promises enumeration. ICollection adds count and mutation operations, and IList adds indexed access. I expose the least powerful abstraction that the caller actually needs.

## 13. Generics 的作用是什么？【P0】

**中文解释：** 泛型让同一套类型或算法用于不同数据类型，同时保留编译期类型安全，减少强制转换和装箱。泛型约束如 `where T : class` 或 `where T : new()` 可以声明算法对类型参数的要求。

**English answer:** Generics let one implementation work with multiple types while preserving compile-time type safety. They reduce casts and often avoid boxing. Constraints communicate and enforce what operations a type parameter must support.

## 14. covariance 和 contravariance 是什么？【P2】

**中文解释：** 协变 `out` 允许把更具体的泛型类型当作更一般的类型使用，例如 `IEnumerable<string>` 可赋给 `IEnumerable<object>`；逆变 `in` 方向相反，常见于接收参数的比较器或委托。它们只适用于引用类型转换和声明为变体的接口、委托。

**English answer:** Covariance lets a more derived generic type be used as a less derived one, such as IEnumerable<string> as IEnumerable<object>. Contravariance reverses that direction for input-oriented types. In and out annotations make the allowed direction explicit.

## 15. delegate 是什么？【P0】

**中文解释：** 委托是类型安全的方法引用，可以把行为当成参数传递或保存。一个委托可以指向静态方法、实例方法或 lambda，也可以组合多个方法。`Action` 和 `Func` 是常用的泛型委托。

**English answer:** A delegate is a type-safe reference to one or more methods. It allows behavior to be passed as data and is the basis for callbacks, events, and many LINQ APIs. Action and Func cover many common signatures.

## 16. event 和 delegate 有什么区别？【P0】

**中文解释：** event 基于 delegate，但限制外部代码只能订阅或取消订阅，不能直接触发或覆盖订阅列表。通常发布者在类型内部触发事件，订阅者响应。订阅者生命周期更长时，要及时退订以避免对象无法被回收。

**English answer:** An event is built on a delegate but restricts outside code to subscribing and unsubscribing. Only the publisher normally raises the event. Long-lived publishers can keep subscribers alive, so unsubscription may be important.

## 17. lambda expression 和 anonymous method 是什么关系？【P1】

**中文解释：** 两者都能创建匿名函数并转换成委托；lambda 语法更简洁，还可以转换成 expression tree。lambda 捕获外部变量时会形成 closure，被捕获变量的生命周期可能延长。

**English answer:** Both lambdas and anonymous methods create unnamed functions, but lambdas are more concise and can also become expression trees. A lambda that captures local variables creates a closure, which can extend their lifetime.

## 18. LINQ 的 deferred execution 是什么？【P0】

**中文解释：** 很多 LINQ 操作只构建查询，直到枚举、`ToList`、`First` 等终结操作时才执行。优点是可以组合查询，风险是重复枚举会重复计算，而且数据源变化可能改变结果。需要稳定快照时应物化。

**English answer:** Many LINQ operators build a query without executing it immediately. Execution happens when the sequence is enumerated or materialized. This enables composition, but repeated enumeration can repeat work and observe changed source data.

## 19. `Select` 和 `SelectMany` 有什么区别？【P1】

**中文解释：** `Select` 对每个元素做一次投影，可能得到嵌套集合；`SelectMany` 在投影后把多个子集合拍平成一个序列。例如订单列表中的所有明细通常用 `SelectMany(order => order.Items)`。

**English answer:** Select maps each source item to one result and can produce nested collections. SelectMany maps and then flattens the child sequences into one sequence. It is useful for retrieving all items across many parent objects.

## 20. `First`、`FirstOrDefault`、`Single` 和 `SingleOrDefault` 怎么选？【P0】

**中文解释：** `First` 要求至少一个结果；`FirstOrDefault` 没有结果时返回默认值；`Single` 还要求恰好一个，多于一个也抛异常；`SingleOrDefault` 允许零个但不允许多个。是否要验证唯一性决定用 First 还是 Single。

**English answer:** First requires at least one match, while FirstOrDefault allows none. Single verifies that exactly one match exists, and SingleOrDefault allows zero or one. I use Single only when multiple matches indicate a data or logic error.

## 21. exception 应该在哪里捕获？【P0】

**中文解释：** 只在能够恢复、补充上下文、转换成领域错误或统一映射响应的层捕获。不要到处 `catch (Exception)` 后吞掉异常。Web API 通常用全局异常处理中间件统一记录并生成安全响应，业务层只处理它真正理解的异常。

**English answer:** I catch an exception only where I can recover, add useful context, translate it, or map it to a response. I avoid swallowing general exceptions. In a Web API, unexpected failures are usually handled centrally.

## 22. `throw;` 和 `throw ex;` 有什么区别？【P0】

**中文解释：** 在 `catch` 中用 `throw;` 会保留原始堆栈；`throw ex;` 会把抛出位置重置到当前代码，丢失关键诊断上下文。因此重新抛出同一异常通常使用裸 `throw;`。

**English answer:** Bare throw rethrows the current exception and preserves its original stack trace. Throw ex resets the apparent throw location and makes diagnosis harder. I normally use bare throw when propagating the same exception.

## 23. `using` 和 `IDisposable` 解决什么问题？【P0】

**中文解释：** `IDisposable` 用于确定性释放文件句柄、数据库连接等外部资源；`using` 会编译成带 `finally` 的释放逻辑，即使发生异常也会调用 `Dispose`。它不是用来代替 GC 回收普通托管对象。

**English answer:** IDisposable provides deterministic cleanup for resources such as streams and database connections. A using statement ensures Dispose is called even if an exception occurs. It complements the GC rather than replacing it.

## 24. nullable value type 和 nullable reference type 有什么不同？【P0】

**中文解释：** `int?` 是 `Nullable<int>`，让值类型可以表示缺失；nullable reference types 是编译器静态分析功能，`string?` 表示允许 null，主要通过警告帮助发现空引用风险，并不改变 CLR 中引用类型的运行时表示。

**English answer:** A nullable value type such as int? is Nullable<int> and can represent no value. Nullable reference types are compiler annotations and flow analysis; string? communicates that null is allowed but does not create a new runtime type.

## 25. record 和 class 有什么区别？【P1】

**中文解释：** record 默认提供基于数据的值相等、友好的打印和 `with` 非破坏性复制，适合 DTO 和值对象；普通 class 默认按引用身份相等。record 仍可以是可变的，所以是否不可变取决于成员设计。

**English answer:** Records provide value-based equality and convenient non-destructive copying, which is useful for DTOs and value objects. Classes use reference identity by default. A record is not automatically immutable; its properties determine that.

## 26. `const`、`readonly` 和 `static readonly` 有什么区别？【P1】

**中文解释：** `const` 必须在编译期确定，并会被内联到调用方；实例 `readonly` 可在声明或构造函数中赋值，每个对象一份；`static readonly` 每个类型一份，可在静态构造阶段计算。跨程序集可能变化的公共值通常不用 `const`。

**English answer:** Const is a compile-time constant and is inlined into calling code. Readonly can be assigned during instance construction, while static readonly is initialized once for the type. I avoid public const for values that may change across library versions.

## 27. extension method 是什么？【P1】

**中文解释：** 扩展方法是在静态类中定义的静态方法，通过第一个带 `this` 的参数，让调用语法看起来像实例方法。它不会真正修改原类型，也不能访问 private 成员；同名实例方法优先于扩展方法。

**English answer:** An extension method is a static method that can be called with instance-method syntax. It does not modify the original type or gain access to private members. A real instance member wins if the signatures conflict.

## 28. `ref`、`out` 和 `in` 参数有什么区别？【P1】

**中文解释：** `ref` 按引用传递且调用前必须赋值，方法可读写；`out` 调用前不必赋值，但方法返回前必须赋值；`in` 按只读引用传递，适合避免复制较大的 struct。普通业务代码应优先清晰返回值，不要滥用引用参数。

**English answer:** Ref passes an initialized variable by reference for reading and writing. Out requires the method to assign it, while in passes a read-only reference. I use them only when the API semantics or performance benefit is clear.

## 29. shallow copy 和 deep copy 有什么区别？【P1】

**中文解释：** 浅复制只复制对象的字段，引用字段仍指向同一个子对象；深复制还复制整个对象图中的相关对象。C# 没有通用、安全、自动的深复制语义，通常为明确模型编写复制逻辑或映射。

**English answer:** A shallow copy duplicates the top-level fields but still shares referenced child objects. A deep copy duplicates the relevant object graph. Because deep-copy semantics are domain-specific, I prefer explicit copying or mapping.

## 30. SOLID 中最常用于 Web API 的原则是什么？【P1】

**中文解释：** 五项都重要，但单一职责、依赖倒置和接口隔离最常落地：Controller 只处理 HTTP 协议，业务逻辑放服务层；高层逻辑依赖抽象并通过 DI 注入；接口保持小而聚焦。不要为了“符合 SOLID”提前制造大量无价值接口。

**English answer:** In Web APIs I often apply single responsibility by keeping controllers thin, dependency inversion through injected abstractions, and interface segregation with focused contracts. SOLID is guidance for managing change, not a reason to add layers without a concrete need.

