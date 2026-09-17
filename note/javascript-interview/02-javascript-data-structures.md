数据结构是组织和保存数据的方式。选择合适的数据结构，可以让插入、删除、查找和遍历等操作更符合业务需要。

JavaScript 中常见的数据结构包括：数组、栈、队列、链表、字典、哈希表、树、图和堆。下面重点整理资料中展开说明的前六种结构。

## 1. 数组 Array

数组按照索引保存一组数据。在 JavaScript 中，数组本质上是特殊对象，索引通常从 `0` 开始：

```js
const users = ["Alice", "Bob", "Carol"];

console.log(users[0]); // Alice
console.log(users.length); // 3
```

数组适合按索引读取元素、按顺序遍历数据，以及保存长度会动态变化的集合：

```js
const colors = ["red", "green"];

colors.push("blue");
colors.splice(1, 1);

console.log(colors); // ["red", "blue"]
```

数组中间插入或删除元素时，后续索引通常需要调整。因此，如果一个场景需要非常频繁地在集合中间插入、删除数据，可以进一步考虑链表等结构。

> 说明：不能笼统地说“数组一定很慢，链表一定更快”。数组按索引访问通常很方便，现代 JavaScript 引擎也会优化常见数组操作；应根据具体操作模式选择结构。

## 2. 栈 Stack

栈是一种“后进先出”（LIFO，Last In First Out）的数据结构。最后放入栈中的元素，会最先被取出。

可以使用数组的 `push()` 和 `pop()` 模拟栈：

```js
const stack = [];

stack.push("A");
stack.push("B");
stack.push("C");

console.log(stack.pop()); // C
console.log(stack.pop()); // B
```

常见使用场景包括函数调用栈、撤销操作、浏览过程中的返回路径，以及括号匹配与表达式计算。

```js
function isBalanced(text) {
  const stack = [];

  for (const char of text) {
    if (char === "(") stack.push(char);
    if (char === ")" && stack.pop() !== "(") return false;
  }

  return stack.length === 0;
}

console.log(isBalanced("(()())")); // true
```

## 3. 队列 Queue

队列是一种“先进先出”（FIFO，First In First Out）的数据结构。最先进入队列的元素，会最先离开。

```js
const queue = [];

queue.push("任务 A");
queue.push("任务 B");
queue.push("任务 C");

console.log(queue.shift()); // 任务 A
console.log(queue.shift()); // 任务 B
```

常见使用场景包括任务排队、消息处理、打印队列和广度优先搜索。

数组的 `shift()` 会移动后续元素。数据量很大时，可以使用头指针，避免每次真正删除数组开头的元素：

```js
class Queue {
  constructor() {
    this.items = [];
    this.head = 0;
  }

  enqueue(value) {
    this.items.push(value);
  }

  dequeue() {
    if (this.head >= this.items.length) return undefined;
    return this.items[this.head++];
  }
}
```

## 4. 链表 Linked List

链表由一系列节点组成。每个节点保存自己的数据，以及指向下一个节点的引用：

```js
const first = { value: "A", next: null };
const second = { value: "B", next: null };

first.next = second;
```

可以把单向链表理解为：

```text
A -> B -> C -> null
```

链表插入节点时，通常只需要调整相邻节点之间的引用：

```js
const nodeA = { value: "A", next: null };
const nodeC = { value: "C", next: null };
const nodeB = { value: "B", next: nodeC };

nodeA.next = nodeB;
```

链表适合频繁插入和删除节点的场景，但要读取第 `n` 个节点时，通常必须从头开始逐个访问，不像数组那样可以直接通过索引读取。

## 5. 字典 Dictionary

字典用“键—值”关系保存数据。JavaScript 普通对象和 `Map` 都可以承担字典的角色。

使用对象：

```js
const phoneBook = {
  Alice: "604-000-0001",
  Bob: "604-000-0002",
};

console.log(phoneBook.Alice);
```

使用 `Map`：

```js
const phoneBook = new Map();

phoneBook.set("Alice", "604-000-0001");
phoneBook.set("Bob", "604-000-0002");

console.log(phoneBook.get("Alice"));
```

`Map` 的键不局限于字符串和 Symbol，也可以使用对象等其他值：

```js
const user = { id: 1 };
const permissions = new Map();

permissions.set(user, ["read", "write"]);

console.log(permissions.get(user));
```

## 6. 哈希表 Hash Table

哈希表也是通过键访问值。它会先用哈希函数把键转换为一个位置，再把数据保存到对应位置。

```text
键 -> 哈希函数 -> 数组位置 -> 值
```

不同的键可能计算出相同位置，这种情况叫做“哈希冲突”。常见处理方法包括：

- 链地址法：同一个位置保存一组键值对；
- 开放寻址法：发生冲突时继续寻找下一个可用位置；
- 线性探测：按照固定顺序逐个检查后续位置。

下面是一个使用链地址法的简化示例：

```js
class HashTable {
  constructor(size = 16) {
    this.buckets = Array.from({ length: size }, () => []);
  }

  hash(key) {
    let total = 0;

    for (const char of String(key)) {
      total += char.charCodeAt(0);
    }

    return total % this.buckets.length;
  }

  set(key, value) {
    const bucket = this.buckets[this.hash(key)];
    const pair = bucket.find(([savedKey]) => savedKey === key);

    if (pair) {
      pair[1] = value;
    } else {
      bucket.push([key, value]);
    }
  }

  get(key) {
    const bucket = this.buckets[this.hash(key)];
    return bucket.find(([savedKey]) => savedKey === key)?.[1];
  }
}
```

```js
const table = new HashTable();

table.set("name", "Alice");
table.set("age", 28);

console.log(table.get("name")); // Alice
```

哈希表适合根据明确的键查找值。它不擅长的是没有键、只能按值做范围或模糊条件的查询。

> 说明：资料中“哈希表不适合搜索”更准确的理解是：哈希表不适合按值进行范围查询、排序或模糊搜索；如果已经知道键，哈希表恰恰通常能提供很快的查询。

## 7. 如何选择

| 需求 | 更适合的数据结构 |
| --- | --- |
| 按索引读取、顺序遍历 | 数组 |
| 后进先出 | 栈 |
| 先进先出 | 队列 |
| 频繁调整节点关系 | 链表 |
| 根据键保存和读取值 | 字典、哈希表 |
| 表达层级关系 | 树 |
| 表达多对多连接关系 | 图 |
| 快速取得最大值或最小值 | 堆 |

实际项目中，这些结构经常组合使用。例如，图的邻接关系可以用字典保存，队列可以用于图的广度优先遍历。

## 8. 总结

- 数组适合按索引访问和顺序遍历。
- 栈遵循后进先出，队列遵循先进先出。
- 链表通过节点之间的引用组织数据。
- 字典用键值关系保存数据，JavaScript 中可以使用对象或 `Map`。
- 哈希表通过哈希函数定位数据，并需要处理哈希冲突。
- 数据结构没有绝对的快慢，应该根据最常见的读取、插入、删除和查询方式选择。
