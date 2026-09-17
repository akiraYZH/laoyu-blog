Ajax（Asynchronous JavaScript and XML）是一种在不重新加载整个页面的情况下，与服务器交换数据并更新局部页面内容的技术。名称里虽然包含 XML，实际项目也常使用 JSON、文本或其他格式。

## 1. Ajax 的组成

传统 Ajax 主要结合：

- `XMLHttpRequest`：向服务器发送 HTTP 请求；
- JavaScript：处理响应和页面逻辑；
- DOM：把返回结果更新到页面。

```text
页面 JavaScript
  ↓ 创建并发送 XHR
服务器处理 HTTP 请求
  ↓ 返回数据
XHR 接收响应
  ↓
JavaScript 使用 DOM 更新页面
```

## 2. Ajax 的工作步骤

资料把基本流程整理为：

1. 创建 `XMLHttpRequest` 对象；
2. 调用 `open()` 配置请求；
3. 调用 `send()` 发送请求；
4. 监听 `readystatechange`；
5. 当请求完成时检查 HTTP 状态；
6. 读取响应数据并更新页面。

## 3. 创建 XMLHttpRequest

```js
const xhr = new XMLHttpRequest();
```

此时只创建了请求对象，还没有确定 HTTP 方法和地址，也没有发送网络请求。

## 4. open

`open()` 的形式是：

```js
xhr.open(method, url, async, user, password);
```

参数含义：

- `method`：请求方法，例如 `GET`、`POST`；
- `url`：请求地址；
- `async`：是否异步，默认通常为 `true`；
- `user`、`password`：可选的 HTTP 认证信息。

```js
xhr.open("GET", "/api/articles", true);
```

`open()` 只是初始化请求，不会真正发送数据。

## 5. send

```js
xhr.send(body);
```

`body` 是请求体。GET 请求通常把参数放在 URL 查询字符串中，并发送 `null`：

```js
xhr.open("GET", "/api/articles?page=1", true);
xhr.send(null);
```

POST 请求可以发送 JSON：

```js
xhr.open("POST", "/api/articles", true);
xhr.setRequestHeader("Content-Type", "application/json");
xhr.send(JSON.stringify({ title: "Ajax" }));
```

也可以发送 `FormData`：

```js
const formData = new FormData();
formData.append("title", "Ajax");

xhr.open("POST", "/api/articles", true);
xhr.send(formData);
```

发送 `FormData` 时通常不需要手动设置 `Content-Type`，浏览器会连同 boundary 自动生成。

## 6. readyState

`XMLHttpRequest.readyState` 表示请求当前状态。状态变化时会触发 `readystatechange`。

| 值 | 状态 | 含义 |
| --- | --- | --- |
| 0 | UNSENT | 已创建，但尚未调用 `open()` |
| 1 | OPENED | 已调用 `open()` |
| 2 | HEADERS_RECEIVED | 已收到响应头 |
| 3 | LOADING | 正在接收响应体 |
| 4 | DONE | 请求完成 |

只有 `readyState === 4`，才表示请求过程已经结束；是否成功还要检查 HTTP 状态码。

## 7. 处理响应

```js
const request = new XMLHttpRequest();

request.onreadystatechange = function () {
  if (request.readyState === 4) {
    if (request.status >= 200 && request.status < 300) {
      console.log(request.responseText);
    } else {
      console.log(`请求失败：${request.status}`);
    }
  }
};

request.open("POST", "https://example.com/api");
request.send();
```

资料示例使用 `200` 到 `300` 判断成功，更准确的常见区间是 `status >= 200 && status < 300`。

`responseText` 返回文本内容。响应是 JSON 时，可以手动解析：

```js
const data = JSON.parse(request.responseText);
```

也可以提前设置：

```js
request.responseType = "json";
```

完成后通过 `request.response` 读取对象。

## 8. 封装 ajax 函数

下面按照资料中的 `options` 结构封装 GET 和 POST：

```js
function ajax(options = {}) {
  const xhr = new XMLHttpRequest();
  const method = (options.type || "GET").toUpperCase();
  const dataType = options.dataType || "json";
  const data = options.data || {};

  let url = options.url;
  let body = null;

  if (method === "GET") {
    const query = new URLSearchParams(data).toString();
    if (query) {
      url += `${url.includes("?") ? "&" : "?"}${query}`;
    }
  } else if (method === "POST") {
    body = JSON.stringify(data);
  }

  xhr.open(method, url, true);

  if (method === "POST") {
    xhr.setRequestHeader("Content-Type", "application/json");
  }

  xhr.onreadystatechange = function () {
    if (xhr.readyState !== 4) return;

    if (xhr.status >= 200 && xhr.status < 300) {
      let result = xhr.responseText;

      if (dataType === "json" && result) {
        result = JSON.parse(result);
      }

      options.success?.(result, xhr.responseXML);
    } else {
      options.fail?.(xhr.status);
    }
  };

  xhr.send(body);

  return xhr;
}
```

调用方式：

```js
ajax({
  type: "post",
  dataType: "json",
  data: { title: "Ajax" },
  url: "https://example.com/api/articles",
  success(data) {
    console.log(data);
  },
  fail(status) {
    console.log(status);
  },
});
```

## 9. 资料原封装中的关键问题

### 9.1 先注册事件，再发送请求

事件处理程序应在 `send()` 前设置，避免极快响应在监听器注册前就完成：

```js
xhr.onreadystatechange = handleStateChange;
xhr.open("GET", url, true);
xhr.send(null);
```

### 9.2 GET 参数要编码

不能直接把普通对象拼接成 `?${params}`：

```js
const query = new URLSearchParams({
  keyword: "JavaScript Ajax",
  page: "1",
});
```

这样可以正确处理空格和特殊字符。

### 9.3 POST 请求体与 Content-Type 要一致

发送 JSON 时设置 `application/json`；发送 URL 编码表单或 `FormData` 时使用对应格式。服务端必须按相同格式解析。

## 10. 取消请求

`XMLHttpRequest.abort()` 可以取消仍在进行的请求：

```js
const request = ajax(options);

request.abort();
```

## 11. 总结

1. Ajax 允许页面在不整体刷新的情况下与服务器交换数据。
2. XHR 的基本流程是 `new XMLHttpRequest()`、`open()`、注册回调、`send()`。
3. `readyState === 4` 表示请求完成，还要通过 HTTP 状态码判断成功或失败。
4. GET 参数放在 URL 中，POST 请求体格式要与 `Content-Type` 一致。
5. `responseText` 是文本，JSON 需要解析或设置 `responseType = "json"`。
6. 封装时要处理参数编码、事件注册顺序、错误状态和取消请求。
