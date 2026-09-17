正则表达式用一套模式描述字符串规则，常用于查找、验证、替换和拆分文本。JavaScript 可以使用正则字面量或 `RegExp` 构造函数创建正则。

## 1. 创建正则表达式

### 1.1 字面量

```js
const re = /\d+/g;
```

### 1.2 RegExp 构造函数

```js
const re = new RegExp("\\d+", "g");

const rule = "\\d+";
const re1 = new RegExp(rule, "g");
```

字符串本身也会处理反斜线，所以在构造函数字符串中，正则的 `\d` 要写成 `"\\d"`。规则来自变量时，构造函数更方便；固定规则通常用字面量更清楚。

```js
const literal = /pattern/flags;
const dynamic = new RegExp("pattern", "flags");
```

## 2. 常用字符与量词

常见字符类：

| 写法 | 含义 |
| --- | --- |
| `\d` | 数字 |
| `\D` | 非数字 |
| `\w` | 字母、数字、下划线等单词字符 |
| `\W` | 非单词字符 |
| `\s` | 空白字符 |
| `.` | 除换行外的任意字符，配合 `s` 时也匹配换行 |

常见量词：

| 写法 | 含义 |
| --- | --- |
| `*` | 0 次或更多次 |
| `+` | 1 次或更多次，相当于 `{1,}` |
| `?` | 0 次或 1 次，相当于 `{0,1}` |
| `{n}` | 恰好 n 次 |
| `{n,m}` | n 到 m 次 |
| `{n,}` | 至少 n 次 |

```js
const reg = /ab{1,3}c/;

reg.test("abc"); // true
reg.test("abbc"); // true
reg.test("abbbc"); // true
```

量词默认是贪婪的，会尽可能多匹配。量词后加 `?` 变成惰性匹配：

```js
const string = "12345";

console.log(string.match(/(\d{1,3})(\d{1,3})/));
// ["12345", "123", "45", ...]

console.log(string.match(/(\d{1,3}?)(\d{1,3})/));
// ["1234", "1", "234", ...]
```

## 3. 分组、选择与边界

括号建立分组：

```text
(beyond){3}：整个 beyond 重复三次
beyond{3}：只有最后的 d 重复三次
```

竖线 `|` 表示选择：

```js
/(abc|xxx)/;
```

`^` 表示字符串开头，`$` 表示字符串结尾：

```js
/^JavaScript$/;
```

分组内容还可以在替换字符串中通过 `$1`、`$2` 引用：

```js
const str = "John Smith";

console.log(str.replace(/(john) (smith)/i, "$2, $1"));
// "Smith, John"
```

## 4. 标志 flags

| 标志 | 含义 |
| --- | --- |
| `g` | 全局匹配 |
| `i` | 忽略大小写 |
| `m` | 多行模式 |
| `s` | 让 `.` 匹配换行符 |
| `u` | Unicode 模式 |
| `y` | 粘连模式，从 `lastIndex` 指定位置开始匹配 |

```js
const re = /javascript/gi;
```

## 5. String.prototype.match

没有 `g` 时，`match()` 返回完整匹配、捕获组、索引和原输入：

```js
const str = "I love JavaScript";
const result = str.match(/Java(Script)/);

console.log(result[0]); // "JavaScript"
console.log(result[1]); // "Script"
console.log(result.length); // 2
console.log(result.index); // 7
console.log(result.input); // "I love JavaScript"
```

带 `g` 时，主要返回全部完整匹配，不再按同样形式返回捕获组详情：

```js
const result = str.match(/Java(Script)/g);

console.log(result[0]); // "JavaScript"
console.log(result.length); // 1
```

没有匹配时返回 `null`：

```js
console.log(str.match(/HTML/)); // null
```

## 6. matchAll

`matchAll()` 可以取得所有匹配及每次捕获组，通常配合全局正则：

```js
const regexp = /t(e)(st(\d?))/g;
const str = "test1test2";
const array = [...str.matchAll(regexp)];

console.log(array[0]); // ["test1", "e", "st1", "1", ...]
console.log(array[1]); // ["test2", "e", "st2", "2", ...]
```

## 7. search、replace 与 split

`search()` 返回第一个匹配位置，找不到返回 `-1`：

```js
const str = "A drop of ink may make a million think";

console.log(str.search(/ink/i)); // 10
```

`replace()` 是否替换全部匹配取决于正则是否带 `g`：

```js
const text = "hello Javascript Javascript Javascript";

console.log(text.replace(/javascript/i, "js"));
// "hello js Javascript Javascript"

console.log(text.replace(/javascript/gi, "js"));
// "hello js js js"
```

`split()` 可以使用正则分隔字符串：

```js
console.log("12, 34, 56".split(/,\s*/));
// ["12", "34", "56"]
```

## 8. RegExp.prototype.exec

`exec()` 返回一次匹配的详细结果。全局正则会更新自己的 `lastIndex`，所以可以循环查找：

```js
const str = "More about JavaScript at https://javascript.info";
const regexp = /javascript/gi;

let result;

while ((result = regexp.exec(str))) {
  console.log(`Found ${result[0]} at position ${result.index}`);
}
```

输出：

```text
Found JavaScript at position 11
Found javascript at position 33
```

## 9. RegExp.prototype.test

`test()` 只返回是否匹配：

```js
const str = "I love JavaScript";

console.log(/love/i.test(str)); // true
```

如果只关心真假，`test()` 比 `match()` 更直接。

## 10. 常见验证规则

资料给出了 QQ 号和用户名示例。

QQ 号：第一位不能为 0，总长度 5 到 15 位：

```js
const qqPattern = /^[1-9][0-9]{4,14}$/;

console.log(qqPattern.test("12345")); // true
```

用户名：以字母开头，后面允许字母、数字、点和下划线，总长度 5 到 20：

```js
const usernamePattern = /^[a-zA-Z]([a-zA-Z0-9]|[._]){4,19}$/;
```

## 11. 解析 URL

资料通过命名捕获组解析 URL：

```js
const protocol = "(?<protocol>https?:)";
const host = "(?<host>(?<hostname>[^/#?:]+)(?::(?<port>\\d+))?)";
const path = "(?<pathname>(?:\\/[^/#?]+)*\\/?)";
const search = "(?<search>(?:\\?[^#]*)?)";
const hash = "(?<hash>(?:#.*)?)";

const reg = new RegExp(
  `^${protocol}//${host}${path}${search}${hash}$`,
);

function execURL(url) {
  const result = reg.exec(url);

  if (!result) {
    return {
      protocol: "",
      host: "",
      hostname: "",
      port: "",
      pathname: "",
      search: "",
      hash: "",
    };
  }

  return {
    ...result.groups,
    port: result.groups.port || "",
  };
}
```

```js
console.log(execURL("https://localhost:8080/?a=b#xxxx"));
```

会得到协议、主机、主机名、端口、路径、查询字符串和片段。

> 说明：学习正则分组时这个例子很有价值；正式解析 URL 时优先使用浏览器内置的 `URL` 和 `URLSearchParams`，它们对边界情况处理更完整。

## 12. 解析查询参数

资料使用带 `y` 标志的正则逐项读取参数：

```js
function execUrlParams(str) {
  str = str.replace(/^[#?&]/, "");
  const result = {};

  if (!str) return result;

  const reg = /(?:^|&)([^&=]*)=?([^&]*?)(?=&|$)/y;
  let match = reg.exec(str);

  while (match) {
    result[match[1]] = match[2];
    match = reg.exec(str);
  }

  return result;
}
```

```js
console.log(execUrlParams("?q=3606&src=srp"));
// { q: "3606", src: "srp" }
```

## 13. 总结

1. 固定正则使用字面量，动态规则使用 `RegExp` 构造函数并注意双重转义。
2. 量词默认贪婪，后加 `?` 可变为惰性匹配。
3. `match()`、`matchAll()`、`search()`、`replace()`、`split()` 是常见字符串正则方法。
4. `exec()` 返回详细匹配并配合 `lastIndex` 迭代，`test()` 只返回真假。
5. 正则适合验证和文本模式匹配；解析标准 URL 时优先使用专用 API。
