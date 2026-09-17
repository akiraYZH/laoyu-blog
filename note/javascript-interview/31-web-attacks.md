前端面试中常见的 Web 安全问题包括 XSS、CSRF 和 SQL 注入。三者攻击目标不同：

- XSS：让恶意脚本进入可信页面并在用户浏览器中执行；
- CSRF：利用用户已经登录的身份，诱导浏览器向目标网站发起非本人意愿的请求；
- SQL 注入：让未经安全处理的输入改变数据库查询结构。

## 1. XSS 是什么

XSS（Cross-Site Scripting，跨站脚本）发生在应用把不可信内容当成 HTML 或 JavaScript 执行时。攻击代码可能读取页面数据、冒用用户操作，或者窃取没有 `HttpOnly` 保护的 Cookie。

## 2. 反射型 XSS

反射型 XSS 常把 URL 参数直接输出到响应页面。资料给出的服务端模板类似：

```html
<input type="text" value="<%= getParameter('keyword') %>" />
<button>搜索</button>

<div>
  你搜索的关键字是：<%= getParameter('keyword') %>
</div>
```

如果用户输入：

```html
"><script>alert('XSS')</script>
```

未经编码直接拼入 HTML 后，攻击者可以闭合原属性并插入脚本。恶意内容来自请求，又立即反射到响应中，所以称为反射型 XSS。

## 3. 存储型 XSS

存储型 XSS 把恶意内容保存到数据库、评论、个人资料等持久化位置。其他用户打开相关页面时，服务端再次输出恶意内容，脚本便在他们的浏览器中执行。

```text
攻击者提交恶意评论
  ↓
服务器把评论保存到数据库
  ↓
其他用户打开页面
  ↓
恶意内容作为 HTML 执行
```

因为一次输入可能影响许多访问者，存储型 XSS 通常危害更大。

## 4. DOM 型 XSS

DOM 型 XSS 主要发生在前端 JavaScript 中：代码读取 `location`、输入框或消息内容，然后写入危险 DOM 接口。

```js
const keyword = new URLSearchParams(location.search).get("keyword");
result.innerHTML = keyword;
```

即使服务器没有把输入拼进 HTML，前端的 `innerHTML` 仍可能让内容作为标记执行。

## 5. 防止 XSS：按输出上下文编码

普通文本中的 `<` 应显示为 `&lt;`。例如内容 `5 < 7` 放入 HTML 文本位置时，可以输出：

```html
<div title="comment">5 &lt; 7</div>
```

但不同上下文需要不同处理：HTML 文本、HTML 属性、URL、JavaScript 字符串、CSS 的安全规则并不完全一样，不能靠一个简单 `escapeHTML()` 解决所有位置。

## 6. 避免危险接口

需要警惕直接执行或解析不可信内容的接口：

- `innerHTML`、`outerHTML`、`document.write()`；
- `eval()`；
- `setTimeout("字符串")`、`setInterval("字符串")`；
- 把不可信内容赋给事件属性，例如 `onclick`、`onerror`、`onload`；
- 未验证就把内容写入 `location` 或链接 `href`。

```js
setTimeout("UNTRUSTED");
setInterval("UNTRUSTED");
location.href = "UNTRUSTED";
eval("UNTRUSTED");
```

显示纯文本时使用：

```js
element.textContent = untrustedText;
```

框架默认的文本插值通常会转义内容，但 Vue 的 `v-html`、React 的 `dangerouslySetInnerHTML` 等显式 HTML 接口仍需要可信或可靠清理后的内容。

其他防线还包括内容安全策略（CSP）、对 Cookie 使用 `HttpOnly`，以及服务端输入验证和输出编码。它们应组合使用，不能把其中一项当作全部防护。

## 7. CSRF 是什么

CSRF（Cross-Site Request Forgery，跨站请求伪造）利用浏览器会自动携带目标网站 Cookie 的特性。用户已经登录银行网站后，又访问攻击页面，攻击页面可以尝试自动提交到银行接口。

```html
<form action="https://bank.example/withdraw" method="POST">
  <input type="hidden" name="account" value="xiaoming" />
  <input type="hidden" name="amount" value="10000" />
  <input type="hidden" name="for" value="hacker" />
</form>

<script>
  document.forms[0].submit();
</script>
```

GET 请求也可能通过链接或图片等资源触发：

```html
<a
  href="https://bank.example/withdraw?amount=1000&for=hacker"
  target="_blank"
>
  查看内容
</a>
```

因此改变状态的操作不应使用 GET，并且服务端不能只因为请求带有登录 Cookie 就认为操作一定来自本人。

## 8. 防止 CSRF

### 8.1 CSRF Token

服务器生成攻击网站无法读取的随机 Token，表单和请求必须同时提交：

```html
<input type="hidden" name="csrfToken" value="tokenValue" />
```

服务端验证 Token 与当前用户会话是否匹配。

### 8.2 SameSite Cookie

```http
Set-Cookie: sessionId=abc; Secure; HttpOnly; SameSite=Lax
```

`SameSite` 可以限制跨站请求自动携带 Cookie。具体选择 `Lax`、`Strict` 或 `None` 要根据登录回调和跨站业务需求决定；`SameSite=None` 通常必须配合 `Secure`。

### 8.3 Origin 与 Referer

服务端可以检查 `Origin` 或 `Referer` 是否来自允许站点。这是额外防线，但不应作为唯一方案，因为某些环境可能不提供完整头信息。

### 8.4 重新验证敏感操作

转账、修改密码等高风险操作可以要求重新输入密码、验证码或使用多因素认证。

## 9. SQL 注入是什么

SQL 注入发生在服务端把用户输入直接拼接进 SQL：

```js
const sql =
  "SELECT * FROM users WHERE username = '" +
  username +
  "' AND password = '" +
  password +
  "'";
```

攻击者可能构造输入改变原查询的语义。SQL 注入的防护责任主要在服务端和数据库访问层，而不是靠前端限制输入。

## 10. 防止 SQL 注入

最重要的是使用参数化查询或 ORM 的参数绑定：

```js
const sql =
  "SELECT * FROM users WHERE username = ? AND password_hash = ?";

db.query(sql, [username, passwordHash]);
```

同时应：

- 验证输入类型、长度和允许范围；
- 数据库账号遵循最小权限；
- 不把数据库错误细节直接返回用户；
- 不依赖前端校验，因为攻击者可以绕过页面直接请求接口。

## 11. 三者对比

| 攻击 | 核心问题 | 主要防线 |
| --- | --- | --- |
| XSS | 不可信内容被当成脚本/HTML 执行 | 按上下文编码、安全 DOM API、清理 HTML、CSP |
| CSRF | 浏览器带着用户会话发起伪造请求 | CSRF Token、SameSite、Origin 验证、敏感操作复核 |
| SQL 注入 | 输入改变 SQL 语句结构 | 参数化查询、ORM 参数绑定、最小权限 |

## 12. 总结

1. XSS 的根源是不可信数据进入可执行上下文。
2. 反射型、存储型和 DOM 型 XSS 的输入路径不同，但都要做上下文安全处理。
3. CSRF 借用用户已有身份，服务端必须验证请求是否来自可信业务流程。
4. 改变状态的操作不要使用 GET，并结合 CSRF Token、SameSite 和来源检查。
5. SQL 注入必须由服务端使用参数化查询防护，前端校验不能代替它。
