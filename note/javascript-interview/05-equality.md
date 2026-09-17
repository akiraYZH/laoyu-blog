JavaScript 同时提供宽松相等 `==` 和严格相等 `===`。二者的核心区别是：`==` 在类型不同时可能先进行类型转换，而 `===` 不进行这种转换。

## 1. `==` 的比较规则

### 1.1 布尔值参与比较

如果其中一个操作数是布尔值，会先把 `true` 转为 `1`、把 `false` 转为 `0`，再继续比较：

```js
const result = true == 1;

console.log(result); // true
```

因此：

```js
false == 0; // true
true == 1; // true
```

### 1.2 字符串和数字比较

如果一边是字符串，另一边是数字，字符串会先转换成数字：

```js
const result = "55" == 55;

console.log(result); // true
```

无法转换成有效数字的字符串不会与普通数字相等：

```js
console.log("hello" == 0); // false
```

### 1.3 对象和基本值比较

对象与字符串或数字比较时，对象会先尝试转换为基本值。资料使用 `valueOf()` 展示这个过程：

```js
const obj = {
  valueOf() {
    return 1;
  },
};

console.log(obj == 1); // true
```

这里不是直接拿整个对象与数字 `1` 比较，而是先取得对象转换后的基本值。

### 1.4 null 与 undefined

在 `==` 比较中，`null` 和 `undefined` 相等：

```js
console.log(null == undefined); // true
```

这是一条专门的比较规则，不意味着它们会普遍转换成同一个数字或字符串。

### 1.5 NaN

只要比较中出现 `NaN`，相等判断就为 `false`。即使两边都是 `NaN` 也不相等：

```js
console.log(NaN == NaN); // false
console.log(NaN === NaN); // false
```

判断一个值是否为 `NaN`，应使用：

```js
console.log(Number.isNaN(NaN)); // true
```

### 1.6 对象与对象

两个对象进行相等比较时，比较的是它们是否指向同一个对象，而不是属性内容是否一样：

```js
const obj1 = { name: "xxx" };
const obj2 = { name: "xxx" };

console.log(obj1 == obj2); // false
```

如果两个变量引用同一个对象，结果才为 `true`：

```js
const obj1 = { name: "xxx" };
const obj2 = obj1;

console.log(obj1 == obj2); // true
console.log(obj1 === obj2); // true
```

## 2. `==` 规则小结

当两个操作数类型不同时，可以按以下顺序理解常见情况：

1. 布尔值先转成数字；
2. 字符串和数字比较时，字符串转成数字；
3. 对象和基本值比较时，对象先转换成基本值；
4. `null == undefined` 为 `true`；
5. `NaN` 与任何值比较都不相等；
6. 两个对象只有引用相同才相等。

## 3. `===` 的比较规则

严格相等不做隐式类型转换。如果两边类型不同，结果直接为 `false`：

```js
console.log("55" === 55); // false
console.log(55 === 55); // true
```

如果类型相同，再比较值是否相同：

```js
console.log("JavaScript" === "JavaScript"); // true
console.log(true === true); // true
```

`null` 和 `undefined` 分别与自身严格相等：

```js
console.log(null === null); // true
console.log(undefined === undefined); // true
```

但它们彼此不是严格相等：

```js
console.log(null == undefined); // true
console.log(null === undefined); // false
```

对于对象，`===` 仍然比较对象引用：

```js
console.log({} === {}); // false

const value = {};
console.log(value === value); // true
```

## 4. 容易出错的例子

下面这些结果集中展示了 `==` 的隐式转换：

```js
"" == "0"; // false
0 == ""; // true
0 == "0"; // true

false == "false"; // false
false == "0"; // true

false == undefined; // false
false == null; // false
null == undefined; // true

" \t\r\n" == 0; // true
```

逐组理解：

- `""` 和 `"0"` 都是字符串，直接比较内容，所以不相等；
- `0 == ""` 时，空字符串被转换成数字 `0`；
- `false == "0"` 时，`false` 先转为 `0`，字符串 `"0"` 也转为数字 `0`；
- 字符串只包含空白字符时，转换成数字得到 `0`；
- `null` 和 `undefined` 的相等关系由专门规则决定。

正因为转换规则容易制造反直觉结果，业务代码通常优先使用 `===`。

## 5. `obj.x == null` 的特殊用法

资料给出一个常见的宽松相等使用场景：

```js
const obj = {};

if (obj.x == null) {
  console.log("obj.x 是 null 或 undefined");
}
```

因为不存在的属性 `obj.x` 是 `undefined`，而 `undefined == null` 为 `true`，所以这个判断可以同时检查 `null` 和 `undefined`。

它等价于更明确的写法：

```js
if (obj.x === null || obj.x === undefined) {
  console.log("obj.x 是 null 或 undefined");
}
```

如果团队不熟悉 `== null` 的规则，显式写出两个条件更容易阅读。

## 6. 应该使用哪一个

通常建议默认使用 `===`：

- 不会在比较过程中悄悄改变类型；
- 结果更容易预测；
- 阅读代码时不需要同时推导隐式转换规则。

只有在明确需要同时判断 `null` 和 `undefined`，并且团队认可这种写法时，才考虑 `value == null`。

## 7. 总结

- `==` 使用宽松相等规则，类型不同时可能发生隐式转换。
- `===` 使用严格相等规则，不进行类型转换。
- `null == undefined` 为 `true`，但 `null === undefined` 为 `false`。
- `NaN` 不等于任何值，包括它自己。
- 对象比较的是引用身份，不会递归比较属性内容。
- 实际开发中默认使用 `===`，可以减少隐式转换造成的误解。
