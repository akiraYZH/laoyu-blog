JavaScript 的类型转换分为显式转换和隐式转换。显式转换由代码主动调用转换函数；隐式转换由运算符或语句根据上下文自动完成。

常见类型包括 `undefined`、`null`、`boolean`、`string`、`number`、`symbol` 和 `object`。类型转换的重点不是背所有例子，而是先判断当前上下文需要布尔值、数字还是字符串。

## 1. 显式转换

显式转换常用三个函数：

```js
Number(value);
String(value);
Boolean(value);
```

## 2. 转换为数字 Number

### 2.1 数字和字符串

```js
Number(324); // 324
Number("324"); // 324
Number("324abc"); // NaN
Number(""); // 0
```

- 数字传入 `Number()` 后保持原值；
- 纯数字字符串转换成对应数字；
- 字符串不能完整转换成数字时得到 `NaN`；
- 空字符串转换成 `0`。

### 2.2 布尔值、undefined 和 null

```js
Number(true); // 1
Number(false); // 0
Number(undefined); // NaN
Number(null); // 0
```

### 2.3 对象和数组

```js
Number({ a: 1 }); // NaN
Number([1, 2, 3]); // NaN
Number([5]); // 5
```

对象转换为数字时，会先尝试转换为基本值。数组转字符串后：

```js
String([1, 2, 3]); // "1,2,3"
String([5]); // "5"
```

因此 `[5]` 最终能转换为数字 `5`，而 `[1, 2, 3]` 产生的字符串不是有效数字。

## 3. Number 与 parseInt 的区别

`Number()` 要求整个字符串能够按数字规则转换，而 `parseInt()` 会从开头解析整数，遇到不能继续解析的字符时停止：

```js
Number("32a3"); // NaN
parseInt("32a3", 10); // 32
```

所以：

- 需要验证整个字符串是数字时，使用 `Number()`；
- 需要从字符串开头读取整数时，可以使用 `parseInt()`，并明确写出进制。

```js
parseInt("101", 2); // 5
parseInt("20px", 10); // 20
```

## 4. 转换为字符串 String

`String()` 可以把各种值转换成字符串：

```js
String(1); // "1"
String("a"); // "a"
String(true); // "true"
String(false); // "false"
String(undefined); // "undefined"
String(null); // "null"
String({ a: 1 }); // "[object Object]"
String([1, 2, 3]); // "1,2,3"
```

普通对象默认使用对象的字符串表示，而数组会把元素用逗号连接。

还可以调用值的 `toString()`：

```js
(123).toString(); // "123"
true.toString(); // "true"
```

但是 `null` 和 `undefined` 没有可直接调用的 `toString()`：

```js
String(null); // "null"
String(undefined); // "undefined"

// null.toString(); // TypeError
// undefined.toString(); // TypeError
```

需要兼容空值时，`String(value)` 更安全。

## 5. 转换为布尔值 Boolean

下面这些值转换为布尔值时是 `false`：

```js
Boolean(undefined); // false
Boolean(null); // false
Boolean(0); // false
Boolean(-0); // false
Boolean(NaN); // false
Boolean(""); // false
```

除这些假值之外，大多数值转换为布尔值是 `true`，包括空对象和空数组：

```js
Boolean({}); // true
Boolean([]); // true
Boolean("false"); // true
Boolean("0"); // true
```

包装对象即使内部包装的是 `false`，对象本身仍然是真值：

```js
Boolean(new Boolean(false)); // true
```

判断真假时，看的是当前值是不是对象，而不是对象内部包装的布尔值。

## 6. 隐式转换发生在哪里

隐式转换常出现在：

- `if`、`while` 等需要布尔条件的位置；
- `==`、`!=`、`>`、`<` 等比较运算；
- `+`、`-`、`*`、`/`、`%` 等算术运算。

```js
if ("hello") {
  console.log("非空字符串是真值");
}
```

条件表达式会把值转换为布尔值。三元表达式也一样：

```js
const x = condition ? 1 : 0;
```

## 7. 加法运算符的转换

`+` 比其他算术运算符特殊：如果运算过程中出现字符串，它会进行字符串拼接。

```js
"5" + 1; // "51"
"5" + true; // "5true"
"5" + false; // "5false"
"5" + {}; // "5[object Object]"
"5" + []; // "5"
"5" + function () {}; // "5function () {}"
"5" + undefined; // "5undefined"
"5" + null; // "5null"
```

逐个理解：

- 数字、布尔值、`undefined`、`null` 会转换为对应字符串；
- 普通对象通常转成 `"[object Object]"`；
- 空数组转成空字符串，所以 `"5" + []` 得到 `"5"`；
- 函数会转换为它的字符串表示。

## 8. 减、乘、除等运算符的转换

`-`、`*`、`/` 等运算符通常要求数字，因此会把操作数转换为数字：

```js
"5" - "2"; // 3
"5" * "2"; // 10
true - 1; // 0
false - 1; // -1
"1" - 1; // 0
"5" * []; // 0
false / "5"; // 0
"abc" - 1; // NaN
null + 1; // 1
undefined + 1; // NaN
```

其中：

- `true` 转为 `1`，`false` 转为 `0`；
- 空数组先转为空字符串，再转为数字 `0`；
- `null` 转为数字 `0`；
- `undefined` 转为数字 `NaN`；
- 非数字字符串转为 `NaN`。

`null + 1` 中没有字符串，双方按数字相加，所以得到 `1`。

## 9. 对象转基本值

对象参与需要基本值的运算时，会尝试通过 `Symbol.toPrimitive`、`valueOf()` 和 `toString()` 得到基本值。资料前面的相等比较也使用了 `valueOf()`：

```js
const obj = {
  valueOf() {
    return 10;
  },
};

console.log(obj - 1); // 9
```

这解释了为什么数组、对象参与运算时会出现字符串或数字结果。

## 10. 总结

1. `Number()`、`String()`、`Boolean()` 用于显式转换。
2. `Number("324abc")` 是 `NaN`，而 `parseInt("324abc", 10)` 可以得到开头的整数 `324`。
3. `undefined` 转数字是 `NaN`，`null` 转数字是 `0`。
4. 空对象和空数组都是真值，`new Boolean(false)` 也是对象，所以是真值。
5. `+` 在出现字符串时倾向于拼接；`-`、`*`、`/` 等通常进行数字转换。
6. 隐式转换规则容易产生反直觉结果，业务代码中应尽量显式转换，让意图清楚。
