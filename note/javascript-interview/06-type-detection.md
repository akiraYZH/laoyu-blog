JavaScript 中常用的类型判断方法包括 `typeof`、`instanceof` 和 `Object.prototype.toString.call()`。三者判断的角度不同，不能互相完全替代。

## 1. typeof

`typeof` 用来返回操作数的类型字符串，有两种写法：

```js
typeof operand;
typeof (operand);
```

常见结果如下：

```js
typeof 1; // "number"
typeof "1"; // "string"
typeof undefined; // "undefined"
typeof true; // "boolean"
typeof Symbol(); // "symbol"
typeof null; // "object"
typeof []; // "object"
typeof {}; // "object"
typeof console; // "object"
typeof console.log; // "function"
```

从这些结果可以看出：

- `typeof` 很适合识别数字、字符串、布尔值、`undefined`、Symbol 和函数；
- `null`、数组和普通对象都会返回 `"object"`；
- 因此只靠 `typeof` 无法进一步区分不同对象。

### 1.1 为什么 typeof null 是 object

`typeof null === "object"` 是 JavaScript 早期实现留下来的历史问题。判断空值时应该直接比较：

```js
if (value === null) {
  console.log("value 是 null");
}
```

不要只写：

```js
if (typeof value === "object") {
  // value 也可能是 null、数组或其他对象
}
```

### 1.2 检查未声明变量

直接访问一个未声明变量会抛出 `ReferenceError`，但对它使用 `typeof` 不会：

```js
if (typeof a !== "undefined") {
  console.log(a);
}
```

这可以用于检查某个全局名称是否存在，但不应靠它掩盖变量拼写错误。

## 2. instanceof

`instanceof` 的语法是：

```js
object instanceof constructor;
```

它判断右侧构造函数的 `prototype` 是否出现在左侧对象的原型链上。

### 2.1 基本示例

```js
function Car() {}

const benz = new Car();

console.log(benz instanceof Car); // true
```

`new Car()` 创建的对象，其原型连接到 `Car.prototype`，所以结果为 `true`。

### 2.2 包装对象与基本字符串

```js
const wrappedString = new String("xxx");
const primitiveString = "xxx";

console.log(wrappedString instanceof String); // true
console.log(primitiveString instanceof String); // false
```

`new String("xxx")` 创建的是对象，原型链中包含 `String.prototype`；而字符串字面量是基本值，不是 `String` 实例。

## 3. 手写 instanceof

根据 `instanceof` 的核心逻辑，可以沿着左侧对象的原型链逐层查找右侧构造函数的 `prototype`：

```js
function myInstanceof(left, right) {
  if (
    left === null ||
    (typeof left !== "object" && typeof left !== "function")
  ) {
    return false;
  }

  let proto = Object.getPrototypeOf(left);

  while (true) {
    if (proto === null) return false;
    if (proto === right.prototype) return true;

    proto = Object.getPrototypeOf(proto);
  }
}
```

验证：

```js
function Car() {}

const benz = new Car();

console.log(myInstanceof(benz, Car)); // true
console.log(myInstanceof({}, Car)); // false
```

查找过程可以表示为：

```text
benz
  ↓ Object.getPrototypeOf
Car.prototype  ← 与右侧的 Car.prototype 比较，匹配则返回 true
  ↓
Object.prototype
  ↓
null            ← 到达 null 仍未匹配则返回 false
```

> 说明：资料示例中的 `Object.getPrototypeof` 大小写有误，正确 API 是 `Object.getPrototypeOf`，其中 `Of` 的首字母都要大写。上面的实现还把函数对象纳入了左侧可判断范围。

## 4. typeof 与 instanceof 的区别

| 对比项 | `typeof` | `instanceof` |
| --- | --- | --- |
| 返回内容 | 类型字符串 | 布尔值 |
| 主要用途 | 判断基本类型和函数 | 判断构造函数原型是否在对象原型链上 |
| `null` | 返回 `"object"` | 不能作为普通对象实例判断 |
| 数组 | 返回 `"object"` | `[] instanceof Array` 为 `true` |
| 基本字符串 | 返回 `"string"` | `"x" instanceof String` 为 `false` |

```js
console.log(typeof []); // "object"
console.log([] instanceof Array); // true

console.log(typeof "xxx"); // "string"
console.log("xxx" instanceof String); // false
```

`instanceof` 适合判断原型关系，但不能替代对基本类型的检测。

## 5. Object.prototype.toString.call

对象原型上的 `toString()` 可以返回形如 `[object Xxx]` 的类型标签。为了确保调用的是 `Object.prototype.toString`，通常配合 `call()` 使用：

```js
Object.prototype.toString.call({}); // "[object Object]"
Object.prototype.toString.call(1); // "[object Number]"
Object.prototype.toString.call("1"); // "[object String]"
Object.prototype.toString.call(true); // "[object Boolean]"
Object.prototype.toString.call(function () {}); // "[object Function]"
Object.prototype.toString.call(null); // "[object Null]"
Object.prototype.toString.call(undefined); // "[object Undefined]"
Object.prototype.toString.call(/123/g); // "[object RegExp]"
Object.prototype.toString.call(new Date()); // "[object Date]"
Object.prototype.toString.call([]); // "[object Array]"
Object.prototype.toString.call(document); // "[object HTMLDocument]"
Object.prototype.toString.call(window); // "[object Window]"
```

相比 `typeof`，这种方法能够继续区分数组、日期、正则、`null` 等值。

## 6. 封装 getType

资料把两种方式结合起来：基本类型和函数直接使用 `typeof`；当 `typeof` 返回 `"object"` 时，再通过 `Object.prototype.toString.call()` 取得更具体的名称。

```js
function getType(obj) {
  const type = typeof obj;

  if (type !== "object") {
    return type;
  }

  return Object.prototype.toString
    .call(obj)
    .replace(/^\[object (\S+)\]$/, "$1");
}
```

调用结果：

```js
getType([]); // "Array"
getType("123"); // "string"
getType(window); // "Window"
getType(null); // "Null"
getType(undefined); // "undefined"
getType(); // "undefined"
getType(function () {}); // "function"
getType(/123/g); // "RegExp"
```

这里可以看到它有意保留了两类返回风格：

- `typeof` 能直接识别时，返回小写字符串，例如 `"string"`、`"undefined"`、`"function"`；
- 需要 `toString` 细分对象时，返回类型标签，例如 `"Array"`、`"Null"`、`"RegExp"`。

## 7. 如何选择

- 判断数字、字符串、布尔值、`undefined`、Symbol、函数：使用 `typeof`；
- 判断某个对象是否继承自指定构造函数：使用 `instanceof`；
- 区分数组、日期、正则、`null` 等对象标签：使用 `Object.prototype.toString.call()`；
- 单独判断数组时，也可以直接使用 `Array.isArray()`。

## 8. 总结

1. `typeof` 简单直接，适合基本类型和函数，但不能细分大多数对象。
2. `typeof null` 为 `"object"` 是历史行为。
3. `instanceof` 检查构造函数的 `prototype` 是否出现在对象原型链中。
4. 基本字符串不是 `String` 构造函数的实例，包装对象才是。
5. `Object.prototype.toString.call()` 能得到更具体的内置类型标签。
6. `getType()` 可以组合 `typeof` 与 `toString`，但要注意返回名称有大小写差异。
