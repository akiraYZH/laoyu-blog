函数缓存（Memoization）是把函数计算结果保存起来。当相同参数再次调用时，直接返回缓存结果，而不是重复执行原函数。

```js
const add = (a, b) => a + b;
const calc = memoize(add);

calc(10, 20); // 第一次计算，得到 30
calc(10, 20); // 直接读取缓存，得到 30
```

## 1. 为什么需要函数缓存

如果函数计算成本较高，而且相同输入经常重复出现，缓存可以减少重复工作。适合的函数通常具备：

- 相同输入产生相同输出；
- 计算过程没有依赖会变化的外部状态；
- 计算成本明显高于缓存查询成本。

```js
function slowSquare(number) {
  // 假设这里包含复杂计算
  return number * number;
}
```

第一次计算后，把参数和结果保存在缓存中，后续相同参数就能复用。

## 2. 闭包是缓存的基础

函数缓存常通过闭包保存 `cache`。先回顾闭包：

```js
(function () {
  var a = 1;

  function add() {
    const b = 2;
    const sum = b + a;
    console.log(sum); // 3
  }

  add();
})();
```

内部函数 `add()` 可以访问外层变量 `a`。

另一个闭包例子：

```js
function foo() {
  var a = 2;

  function bar() {
    console.log(a);
  }

  return bar;
}

const baz = foo();
baz(); // 2
```

即使 `foo()` 已经执行结束，`baz` 引用的函数仍能访问外层变量 `a`。函数缓存同样利用这个特性，让返回的新函数长期访问缓存对象。

## 3. 柯里化形式

资料也用普通加法与分步传参说明返回函数：

```js
const add = function (x, y) {
  return x + y;
};

add(3, 4); // 7
```

```js
const add2 = function (x) {
  return function (y) {
    return x + y;
  };
};

add2(3)(4); // 7
```

`add2(3)` 返回的新函数记住 `x = 3`。Memoization 的结构也类似：外层函数创建缓存并返回一个包装函数。

## 4. 资料中的 memoize 思路

```js
const memoize = function (func, context) {
  const cache = Object.create(null);
  const thisArg = context || this;

  return (...args) => {
    const key = args.join(",");

    if (!(key in cache)) {
      cache[key] = func.apply(thisArg, args);
    }

    return cache[key];
  };
};
```

调用：

```js
const add = (a, b) => a + b;
const calc = memoize(add);

const num1 = calc(100, 200); // 执行 add，并保存 300
const num2 = calc(100, 200); // 直接返回 300
```

执行过程：

1. 调用 `memoize(add)`，创建私有的 `cache`；
2. 返回包装函数 `calc`；
3. 第一次调用 `calc(100, 200)` 时，缓存没有对应键，于是执行 `add`；
4. 把结果保存到 `cache`；
5. 第二次使用相同参数时，直接读取缓存。

## 5. 为什么不能写 if (!cache[key])

资料原始示例使用类似下面的判断：

```js
if (!cache[key]) {
  cache[key] = func.apply(context, args);
}
```

如果函数结果恰好是 `0`、`false` 或空字符串，这些值会被当成“没有缓存”，导致重复计算。应判断键是否存在：

```js
if (!(key in cache)) {
  cache[key] = func.apply(context, args);
}
```

或者使用 `Map.has()`。

## 6. 使用 Map 实现

```js
function memoize(func, context) {
  const cache = new Map();

  return function (...args) {
    const key = JSON.stringify(args);

    if (!cache.has(key)) {
      cache.set(key, func.apply(context ?? this, args));
    }

    return cache.get(key);
  };
}
```

```js
function multiply(a, b) {
  console.log("执行计算");
  return a * b;
}

const cachedMultiply = memoize(multiply);

console.log(cachedMultiply(10, 20)); // 打印“执行计算”，返回 200
console.log(cachedMultiply(10, 20)); // 只返回 200
```

`Map` 可以明确区分“键不存在”和“缓存值是假值”。

## 7. 缓存键的限制

简单的 `args.join(",")` 或 `JSON.stringify(args)` 都不是完美方案。

```js
JSON.stringify([{ a: 1, b: 2 }]);
JSON.stringify([{ b: 2, a: 1 }]);
```

逻辑上接近的对象，属性顺序不同可能得到不同字符串；循环引用还会让 `JSON.stringify()` 抛错。反过来，简单拼接也可能让不同参数产生相同键。

因此缓存实现要根据参数类型选择策略：

- 参数都是数字、短字符串：可以生成稳定字符串键；
- 参数包含对象：可按对象身份使用嵌套 `Map` 或 `WeakMap`；
- 业务有唯一 ID：优先直接使用 ID 作为键。

## 8. 缓存何时失效

如果缓存永远增长，会占用越来越多内存。实际项目通常还要考虑：

- 最大缓存条数；
- 过期时间；
- LRU 等淘汰策略；
- 外部数据发生变化时主动清理缓存。

带副作用或依赖当前时间、随机数、网络状态的函数，不适合直接按输入永久缓存：

```js
function getCurrentTime() {
  return Date.now();
}
```

相同的“无参数输入”并不代表它每次都应该返回同一个值。

## 9. 总结

1. Memoization 保存函数的输入和输出，相同输入再次出现时直接返回缓存。
2. 闭包让包装函数在外层函数结束后继续访问 `cache`。
3. 不能用缓存值的真假判断是否命中，否则 `0`、`false`、空字符串会失效。
4. 参数序列化只是简化方案，包含对象时必须考虑冲突、属性顺序和循环引用。
5. 只缓存输入稳定、结果可复用的计算，并设计清理或淘汰策略。
