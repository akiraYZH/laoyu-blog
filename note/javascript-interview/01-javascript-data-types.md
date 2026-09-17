JavaScript 的数据类型可以分为两组：**基本数据类型**和**引用数据类型**。理解这两组类型，不只是为了记住 `typeof` 的结果，更重要的是理解变量赋值后，修改其中一个变量为什么有时不会影响另一个变量，有时却会共享同一份数据。

## 1. 基本数据类型

基本数据类型包括 `Number`、`Undefined`、`String`、`Null`、`Boolean` 和 `Symbol`。

### 1.1 Number

JavaScript 使用 `Number` 表示整数和浮点数。整数除了十进制，还可以使用八进制和十六进制表示：

```js
const decimal = 55; // 十进制 55
const octal = 0o70; // 八进制，十进制为 56
const hex = 0xA; // 十六进制，十进制为 10
```

浮点数既可以直接写小数，也可以使用科学计数法：

```js
const floatNum1 = 1.1;
const floatNum2 = 0.1;
const floatNum3 = 0.1;
const floatNum4 = 3.125e7; // 31250000
```

`NaN` 的含义是“不是一个有效数字”。例如，某些没有正常数值结果的运算会得到 `NaN`：

```js
console.log(0 / 0); // NaN
console.log(-0 / +0); // NaN
```

### 1.2 Undefined

使用 `var` 或 `let` 声明变量但没有赋值时，变量的值是 `undefined`：

```js
let message;

console.log(message === undefined); // true
console.log(message); // undefined
```

但是，“变量已经声明但没有赋值”和“变量根本没有声明”不是一回事：

```js
let message;

console.log(message); // undefined
console.log(age); // ReferenceError: age is not defined
```

### 1.3 String

字符串可以使用单引号、双引号或模板字符串创建：

```js
const firstName = "John";
const lastName = 'Jacob';
const fullName = `John Jacob`;
```

JavaScript 字符串是不可变的。所谓“不可变”，不是说字符串变量不能重新赋值，而是说原字符串本身不会被原地修改。下面的操作会产生一个新字符串，再把它赋回 `lang`：

```js
let lang = "Java";
lang = lang + "Script";

console.log(lang); // JavaScript
```

### 1.4 Null

`null` 通常表示一个空对象引用，或者开发者有意表达“当前没有值”：

```js
const car = null;

console.log(typeof car); // "object"
```

`typeof null` 得到 `"object"` 是 JavaScript 长期保留的历史行为，并不表示 `null` 真的是普通对象。

在宽松相等比较中，`null` 和 `undefined` 相等：

```js
console.log(null == undefined); // true
```

但是二者语义不同：`undefined` 常表示“尚未赋值或缺少值”，`null` 通常是主动设置的空值。

### 1.5 Boolean

布尔类型只有两个值：`true` 和 `false`。使用 `Boolean()` 转换其他值时，常见规则如下：

| 类型 | 转换为 `true` 的常见值 | 转换为 `false` 的值 |
| --- | --- | --- |
| String | 非空字符串 | 空字符串 `""` |
| Number | 非零数字 | `0`、`-0`、`NaN` |
| Object | 普通对象 | `null` |
| Undefined | 无 | `undefined` |

```js
console.log(Boolean("JavaScript")); // true
console.log(Boolean("")); // false
console.log(Boolean(1)); // true
console.log(Boolean(0)); // false
console.log(Boolean({})); // true
console.log(Boolean(null)); // false
```

### 1.6 Symbol

`Symbol` 用来创建唯一值。即使传入相同描述，每次调用 `Symbol()` 得到的值也不相等：

```js
const genericSymbol = Symbol();
const otherGenericSymbol = Symbol();

console.log(genericSymbol === otherGenericSymbol); // false

const fooSymbol = Symbol("foo");
const otherFooSymbol = Symbol("foo");

console.log(fooSymbol === otherFooSymbol); // false
```

## 2. 引用数据类型

引用数据类型保存的是复杂数据。常见类型包括 `Object`、`Array`、`Function`，以及 `Date`、`RegExp`、`Map`、`Set` 等。

### 2.1 Object

对象由一组键值对组成：

```js
const person = {
  name: "Nicholas",
  age: 29,
  5: true,
};
```

### 2.2 Array

JavaScript 数组可以动态增加元素，而且同一个数组可以保存不同类型的值：

```js
const colors = ["red", 2, { age: 20 }];
colors.push("blue");

console.log(colors);
```

### 2.3 Function

函数也是对象。常见定义方式包括函数声明、函数表达式和箭头函数：

```js
function sum(num1, num2) {
  return num1 + num2;
}
```

```js
const sum = function (num1, num2) {
  return num1 + num2;
};
```

```js
const sum = (num1, num2) => {
  return num1 + num2;
};
```

### 2.4 其他引用类型

JavaScript 还提供了多种内置对象：

```js
const now = new Date();
const pattern = /javascript/i;
const userMap = new Map();
const uniqueValues = new Set();
```

## 3. 基本类型与引用类型的赋值区别

### 3.1 基本类型：复制值

把基本类型变量赋给另一个变量时，会复制当前值。之后修改其中一个变量，不会影响另一个变量：

```js
let a = 10;
let b = a;

b = 20;

console.log(a); // 10
console.log(b); // 20
```

可以把它理解为：`a` 和 `b` 各自拥有一份独立的数字 `10`，后来只是把 `b` 的值改成了 `20`。

### 3.2 引用类型：复制引用

把对象赋给另一个变量时，复制的是对同一个对象的引用。因此，通过任意一个变量修改对象，另一个变量也能看到变化：

```js
const obj1 = {};
const obj2 = obj1;

obj2.name = "Xxx";

console.log(obj1.name); // Xxx
console.log(obj1 === obj2); // true
```

此时 `obj1` 和 `obj2` 不是两个独立对象，而是共同指向同一个对象。

## 4. 栈与堆的常见理解方式

面试资料通常会用“栈和堆”解释两类数据：基本类型的值可以直接理解为保存在变量位置中；引用类型的变量保存对象的引用，而对象本身位于另一块内存区域。

这个模型有助于理解赋值行为：

```text
基本类型：变量 a 保存 10，变量 b 得到 10 的副本
引用类型：变量 obj1 和 obj2 保存同一个对象的引用
```

> 说明：栈与堆是常用的教学模型。ECMAScript 规范定义的是语言行为，并不强制 JavaScript 引擎采用某一种具体的内存存放方式。面试中重点应放在“复制值”和“共享对象引用”的可观察结果上。

## 5. 总结

- 基本数据类型包括 `Number`、`Undefined`、`String`、`Null`、`Boolean` 和 `Symbol`。
- 常见引用数据类型包括 `Object`、`Array`、`Function`、`Date`、`RegExp`、`Map` 和 `Set`。
- 基本类型赋值时复制值，两个变量之后互不影响。
- 引用类型赋值时复制引用，两个变量可能指向同一个对象。
- `typeof null` 为 `"object"` 是历史遗留行为。
- “栈与堆”适合作为理解模型，但实际面试回答应以语言表现出来的赋值和修改结果为准。
