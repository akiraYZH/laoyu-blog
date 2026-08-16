---
title: "为什么 PostgreSQL INSERT 失败后 ID 仍然会增加"
description: "理解 Identity 与 Sequence 的 nextval 行为、事务回滚后的 ID 缺口，以及为什么主键不保证连续。"
tags:
  - PostgreSQL
  - Sequence
  - Identity
  - Transaction
  - Primary Key
---

# 为什么 PostgreSQL INSERT 失败后 ID 仍然会增加

向拥有自增主键和 Unique Index 的表插入数据时，即使 INSERT 最终因为唯一冲突失败，下一次成功记录的 ID 仍可能跳过一个数字。这不是 EF Core 的 Bug，也不表示数据库中丢失了一行数据。

## Sequence 先分配 ID

PostgreSQL 的 Identity 或 Serial Column 通常依赖 Sequence。INSERT 会先取得新值，再完成其他约束检查：

```text
Sequence nextval() → 分配 ID
        ↓
执行 INSERT
        ↓
检查 NOT NULL、Unique Index 等约束
```

假设下一个 ID 是 `5`，但 Slug 已经存在：

```text
nextval() 返回 5
    ↓
Unique Index 发现冲突
    ↓
INSERT 失败
    ↓
ID 5 已经被消耗
```

下一次成功 INSERT 会得到 `6`：

```text
已有记录：1、2、3、4
失败 INSERT：消耗 5
下一条记录：6
```

## 为什么 Transaction 不退回 Sequence

普通数据修改可以随事务回滚，但 `nextval()` 取得的 Sequence Value 不会被收回。PostgreSQL 官方文档明确说明 Sequence 不能用于保证无缺口编号；Transaction Abort 和 `INSERT ... ON CONFLICT` 都可能消耗号码。

这是并发设计的一部分。多个请求可以快速取得不同值：

```text
Request A → 5
Request B → 6
Request C → 7
```

如果 A 失败后必须安全地归还 `5`，数据库就要协调其他并发事务是否已经使用后续号码，并引入更多锁和竞争。

## 主键只保证身份，不保证连续

Primary Key 的职责是唯一识别一行：

```text
唯一：需要
稳定引用：需要
连续：不需要
代表记录数量：不需要
```

以下情况都可能形成缺口：

- INSERT 因约束失败；
- Transaction Rollback；
- `ON CONFLICT`；
- 已有记录被删除；
- 并发事务提前申请 Sequence Value；
- 数据迁移或手动调整 Sequence。

因此不要使用最大 ID 推断记录总数。需要数量时执行：

```csharp
var count = await dbContext.BlogPosts.CountAsync();
```

## 不要为了美观重置 Sequence

生产数据库中不要因为 ID 不连续就回退 Sequence。错误重置可能导致未来生成已经存在的主键，从而产生新的 Unique Violation。

如果业务真的要求连续编号，例如财务单据号，应单独设计业务编号和锁策略：

```text
Database Primary Key → 技术身份，可以有缺口
Invoice Number       → 业务编号，使用独立规则
```

连续业务编号会带来并发、回滚和审计要求，不能简单地依赖普通自增主键。

## 验证现象

1. 创建一条使用新 Slug 的文章，记录返回 ID；
2. 再次创建相同 Slug，使 INSERT 失败；
3. 换一个新 Slug 创建成功；
4. 比较两次成功记录的 ID。

出现一个或多个缺口是正常结果。验证重点应是失败请求没有插入重复数据，而不是 ID 是否连续。

## 总结

```text
Sequence 负责快速生成唯一值
失败事务不会归还 nextval() 的结果
Primary Key 保证唯一，不保证连续
ID 缺口不代表数据丢失
```

## 参考资料

- [PostgreSQL Sequence Functions](https://www.postgresql.org/docs/current/functions-sequence.html)
- [PostgreSQL Serial Types](https://www.postgresql.org/docs/current/datatype-numeric.html#DATATYPE-SERIAL)

