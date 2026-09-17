浏览器常见的客户端存储方式包括 Cookie、`localStorage`、`sessionStorage` 和 IndexedDB。它们在容量、生命周期、数据形式以及是否随 HTTP 请求发送等方面不同。

## 1. Cookie

Cookie 最初主要用于让 HTTP 请求携带少量状态。服务器可以通过响应头设置 Cookie，浏览器之后会在符合条件的请求中自动携带。

```http
Set-Cookie: sessionId=abc123
```

### 1.1 过期时间

`Expires` 指定绝对过期时间：

```http
Set-Cookie: token=abc; Expires=Wed, 21 Oct 2026 07:28:00 GMT
```

`Max-Age` 指定从现在开始还能存活多少秒：

```http
Set-Cookie: token=abc; Max-Age=604800
```

没有设置持久化时间的 Cookie 通常属于会话 Cookie，会在浏览器会话结束时失效。

### 1.2 Domain 与 Path

`Domain` 控制哪些域名可以接收 Cookie，`Path` 控制哪些 URL 路径可以接收：

```http
Set-Cookie: name=aa; Domain=example.com; Path=/docs
```

上例主要匹配 `/docs` 及其子路径。

### 1.3 Secure

带 `Secure` 的 Cookie 只通过 HTTPS 连接发送：

```http
Set-Cookie: sessionId=abc; Secure
```

登录会话通常还应结合 `HttpOnly` 和 `SameSite`：

```http
Set-Cookie: sessionId=abc; Secure; HttpOnly; SameSite=Lax
```

- `HttpOnly`：禁止 JavaScript 读取 Cookie；
- `SameSite`：限制跨站请求携带 Cookie，有助于降低 CSRF 风险。

### 1.4 JavaScript 操作 Cookie

```js
document.cookie = "username=Tom; path=/";
console.log(document.cookie);
```

给 `document.cookie` 赋值不会直接覆盖所有 Cookie，而是新增或更新符合相同名称、Domain 和 Path 的那一个。JavaScript 不能设置其他域名的 Cookie。

## 2. localStorage

`localStorage` 是 HTML5 Web Storage 的一部分。它按源保存字符串键值对，数据不会因为普通页面关闭而自动消失。

### 2.1 写入和读取

```js
localStorage.setItem("username", "cfangxu");

const username = localStorage.getItem("username");
console.log(username); // "cfangxu"
```

### 2.2 取得键名

```js
console.log(localStorage.key(0));
console.log(localStorage.length);
```

### 2.3 删除和清空

```js
localStorage.removeItem("username");
localStorage.clear();
```

`removeItem()` 删除指定键，`clear()` 清空当前源下的全部 `localStorage` 数据。

### 2.4 只能直接保存字符串

```js
localStorage.setItem("key", { name: "value" });
console.log(localStorage.getItem("key")); // "[object Object]"
```

对象被自动转成普通字符串，原结构丢失。应该主动序列化：

```js
const settings = { theme: "dark" };

localStorage.setItem("settings", JSON.stringify(settings));

const savedSettings = JSON.parse(
  localStorage.getItem("settings") ?? "null",
);

console.log(savedSettings.theme); // "dark"
```

## 3. sessionStorage

`sessionStorage` 的 API 与 `localStorage` 基本相同，但数据主要绑定当前标签页的页面会话。

```js
sessionStorage.setItem("step", "2");
console.log(sessionStorage.getItem("step")); // "2"
sessionStorage.removeItem("step");
```

同源的新标签页通常不会像 `localStorage` 那样持续共享后续变化；标签页会话结束后，数据也会被清除。

## 4. IndexedDB

IndexedDB 是浏览器提供的异步、事务型客户端数据库。它适合保存比 Web Storage 更大量、更结构化的数据。

Web Storage 保存字符串键值对，而 IndexedDB 可以保存对象，并通过对象仓库（Object Store）和索引进行查询。

### 4.1 打开数据库

```js
const request = indexedDB.open("notes-db", 1);
```

第一次创建数据库或版本升级时，通过 `onupgradeneeded` 创建对象仓库：

```js
request.onupgradeneeded = function (event) {
  const db = event.target.result;

  if (!db.objectStoreNames.contains("notes")) {
    const store = db.createObjectStore("notes", {
      keyPath: "id",
      autoIncrement: true,
    });

    store.createIndex("title", "title", { unique: false });
  }
};
```

### 4.2 请求对象

IndexedDB 操作通常返回请求对象，通过事件取得结果：

```js
request.onsuccess = function (event) {
  const db = event.target.result;
  console.log("数据库已打开", db.name);
};

request.onerror = function () {
  console.error(request.error);
};
```

实际项目可以使用对 IndexedDB 的封装库，例如资料提到的 GoDB.js，减少底层事件式代码。

## 5. 四种存储方式对比

| 对比项 | Cookie | localStorage | sessionStorage | IndexedDB |
| --- | --- | --- | --- | --- |
| 常见容量 | 约 4KB/条 | 通常数 MB | 通常数 MB | 通常更大，由浏览器配额决定 |
| 生命周期 | 由会话或过期属性决定 | 主动删除前持续保存 | 当前标签页会话 | 主动删除或被浏览器清理前 |
| 数据形式 | 字符串 | 字符串 | 字符串 | 结构化克隆对象 |
| 随请求发送 | 符合规则时自动发送 | 不发送 | 不发送 | 不发送 |
| 访问方式 | 同步字符串 API | 同步 | 同步 | 异步、事务 |

## 6. 如何选择

- 需要服务器自动接收的少量会话信息：Cookie；
- 需要跨页面长期保存少量前端数据：`localStorage`；
- 只在当前标签页会话中使用：`sessionStorage`；
- 需要保存大量结构化数据、离线数据或需要索引查询：IndexedDB。

不要把敏感令牌随意放入可被 JavaScript 读取的 Web Storage。只要页面发生 XSS，攻击代码就可能读取这些数据。

## 7. 总结

1. Cookie 容量小，会按 Domain、Path、Secure 等规则随请求发送。
2. `localStorage` 和 `sessionStorage` 都直接保存字符串，保存对象需要 JSON 序列化。
3. `localStorage` 跨页面持续存在，`sessionStorage` 主要属于当前标签页会话。
4. IndexedDB 是异步的客户端数据库，支持对象仓库、索引和事务。
5. 选择存储方式时同时考虑生命周期、容量、数据结构、网络行为和安全性。
