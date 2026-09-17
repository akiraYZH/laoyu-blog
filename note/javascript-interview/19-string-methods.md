JavaScript 字符串是不可变值。大多数字符串方法不会修改原字符串，而是返回一个新字符串或查询结果。

## 1. 拼接字符串

字符串可以使用 `+`、模板字符串或 `concat()` 拼接。

```js
const firstName = "Tom";
const lastName = "Lee";

const value1 = firstName + " " + lastName;
const value2 = `${firstName} ${lastName}`;
const value3 = firstName.concat(" ", lastName);
```

资料中的 `concat()` 示例：

```js
const stringValue = "hello ";
const result = stringValue.concat("world");

console.log(result); // "hello world"
console.log(stringValue); // "hello "
```

原字符串没有改变。

## 2. 截取字符串

常见方法包括 `slice()`、`substring()` 和 `substr()`。

```js
const stringValue = "hello world";

console.log(stringValue.slice(3)); // "lo world"
console.log(stringValue.substring(3)); // "lo world"
console.log(stringValue.substr(3)); // "lo world"

console.log(stringValue.slice(3, 7)); // "lo w"
console.log(stringValue.substring(3, 7)); // "lo w"
console.log(stringValue.substr(3, 7)); // "lo worl"
```

区别在第二个参数：

- `slice(start, end)`：从 `start` 截取到 `end` 之前；
- `substring(start, end)`：同样使用起止索引，但对负数和参数顺序的处理不同；
- `substr(start, length)`：第二个参数表示长度。

```js
const text = "hello world";

console.log(text.slice(-5)); // "world"
console.log(text.substring(-5)); // "hello world"，负数按 0 处理
```

> 说明：`substr()` 已属于旧式 API，不建议在新代码中继续使用。现代代码通常使用 `slice()`。

## 3. 去除首尾空白

`trim()` 返回去掉字符串两端空白后的新字符串：

```js
const stringValue = " hello world ";
const trimmedStringValue = stringValue.trim();

console.log(stringValue); // " hello world "
console.log(trimmedStringValue); // "hello world"
```

还可以只处理一侧：

```js
" hello ".trimStart(); // "hello "
" hello ".trimEnd(); // " hello"
```

## 4. 重复字符串

`repeat(count)` 把字符串重复指定次数：

```js
const stringValue = "na ";
const copyResult = stringValue.repeat(2);

console.log(copyResult); // "na na "
```

```js
console.log("-".repeat(10)); // "----------"
```

## 5. 补齐字符串

`padStart()` 从开头补齐，`padEnd()` 从末尾补齐：

```js
const stringValue = "foo";

console.log(stringValue.padStart(6)); // "   foo"
console.log(stringValue.padStart(9, ".")); // "......foo"
console.log(stringValue.padEnd(6, ".")); // "foo..."
```

第一个参数是最终字符串长度；第二个参数是用来补齐的字符串，省略时使用空格。

## 6. 大小写转换

```js
const stringValue = "hello world";

console.log(stringValue.toUpperCase()); // "HELLO WORLD"
console.log(stringValue.toLowerCase()); // "hello world"
```

这些方法返回新字符串，不修改原值。

## 7. 取得指定位置字符

`charAt(index)` 返回指定索引的字符：

```js
const message = "abcde";

console.log(message.charAt(2)); // "c"
console.log(message[2]); // "c"
```

## 8. 查找字符串位置

`indexOf()` 返回第一次出现的位置，找不到返回 `-1`：

```js
const stringValue = "hello world";

console.log(stringValue.indexOf("o")); // 4
console.log(stringValue.indexOf("x")); // -1
```

还可以从指定位置开始查找：

```js
console.log("hello world".indexOf("o", 5)); // 7
```

## 9. 判断开头、结尾和包含关系

```js
const message = "foobarbaz";

console.log(message.startsWith("foo")); // true
console.log(message.startsWith("bar")); // false
console.log(message.includes("bar")); // true
console.log(message.includes("qux")); // false
console.log(message.endsWith("baz")); // true
```

- `startsWith()`：是否以指定内容开头；
- `endsWith()`：是否以指定内容结尾；
- `includes()`：是否包含指定内容。

它们直接返回布尔值，比使用 `indexOf(...) !== -1` 更容易表达意图。

## 10. split：拆分字符串

`split(separator)` 根据分隔符把字符串拆成数组：

```js
const str = "12+23+34";
const arr = str.split("+");

console.log(arr); // ["12", "23", "34"]
```

也可以使用正则表达式作为分隔符：

```js
const words = "one, two; three".split(/[,;]\s*/);

console.log(words); // ["one", "two", "three"]
```

## 11. match：匹配正则

`match()` 根据正则表达式取得匹配结果：

```js
const text = "cat, bat, sat, fat";
const pattern = /.at/;
const matches = text.match(pattern);

console.log(matches[0]); // "cat"
```

使用全局标志 `g` 时，可以取得所有匹配字符串：

```js
console.log(text.match(/.at/g));
// ["cat", "bat", "sat", "fat"]
```

## 12. search：查找正则位置

`search()` 返回第一个正则匹配位置，找不到返回 `-1`：

```js
const text = "cat, bat, sat, fat";
const position = text.search(/at/);

console.log(position); // 1
```

它返回索引，不返回匹配内容本身。

## 13. replace：替换内容

```js
const text = "cat, bat, sat, fat";
const result = text.replace("at", "ond");

console.log(result); // "cond, bat, sat, fat"
console.log(text); // 原字符串不变
```

普通字符串或没有 `g` 标志的正则只替换第一个匹配。替换所有匹配可以使用：

```js
console.log(text.replace(/at/g, "ond"));
// "cond, bond, sond, fond"
```

现代环境也可以使用：

```js
console.log(text.replaceAll("at", "ond"));
```

## 14. 总结

1. 拼接：模板字符串、`+`、`concat()`。
2. 截取：优先使用 `slice()`；`substr()` 属于旧式 API。
3. 整理格式：`trim()`、`repeat()`、`padStart()`、`padEnd()`、大小写转换。
4. 查询：`charAt()`、`indexOf()`、`startsWith()`、`endsWith()`、`includes()`。
5. 拆分与正则：`split()`、`match()`、`search()`、`replace()`。
6. 字符串不可变，大多数方法都会返回新值，不会直接修改原字符串。
