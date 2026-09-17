单点登录（Single Sign-On，SSO）让用户在统一认证系统登录一次后，可以访问多个相互信任的应用，而不必在每个应用重复输入账号和密码。

```text
用户 → 统一认证中心 SSO
             ├── Application 1
             ├── Application 2
             └── Application 3
```

## 1. 基本流程

1. 用户访问应用 1，但没有登录状态；
2. 应用 1 把用户重定向到 SSO 认证中心；
3. 用户在认证中心完成登录；
4. 认证中心向应用 1 返回可验证的登录凭据；
5. 应用 1 建立自己的登录会话；
6. 用户访问应用 2 时，认证中心已经有登录状态，可以直接签发应用 2 的凭据。

应用之间不应该互相共享用户密码。它们信任的是认证中心返回并可验证的结果。

## 2. 同一主域下共享 Cookie

Cookie 可以通过 `Domain` 设置作用域。如果多个应用都是同一主域的子域，例如：

```text
tieba.baidu.com
map.baidu.com
www.baidu.com
```

服务器可以把 Cookie 设置到共同主域：

```http
Set-Cookie: sessionId=abc; Domain=.baidu.com; Path=/; Secure; HttpOnly
```

符合 Domain 和 Path 规则的子域请求会自动携带这个 Cookie，因此多个子应用可以识别统一会话。

这个方法的限制是：

- 应用必须位于可以共享 Cookie 的同一主域；
- Cookie 的 Domain 和 Path 必须正确；
- 跨完全不同域名不能直接共享同一个 Cookie；
- 所有相关系统必须共同保护会话安全。

## 3. 重定向传递一次性凭据

不同主域的应用通常通过认证中心重定向完成登录：

```text
应用 A → SSO 登录页
SSO 验证用户
SSO → 应用 A 回调地址，附带一次性 code
应用 A 服务端用 code 换取用户信息
应用 A 创建自己的 Cookie 会话
```

资料用 URL 携带 Token 来解释这个思路。正式系统更适合传递短时、一次性的授权码，而不是把长期 Token 直接放在 URL：

```text
https://app1.example.com/callback?code=one-time-code
```

原因是 URL 可能出现在浏览历史、日志、Referer 或截图中。

## 4. iframe、postMessage 与 localStorage

资料还介绍了跨域页面通过隐藏 `iframe` 和 `postMessage()` 把 Token 发送到目标应用，再由目标页面写入自己的 `localStorage`。

发送端：

```js
const token = result.data.token;

const iframe = document.createElement("iframe");
iframe.src = "https://app1.com/localstorage.html";
iframe.hidden = true;
document.body.append(iframe);

iframe.addEventListener("load", function () {
  iframe.contentWindow.postMessage(token, "https://app1.com");
});
```

接收页运行在 `app1.com`，因此可以写入 `app1.com` 自己的 `localStorage`：

```js
window.addEventListener("message", function (event) {
  if (event.origin !== "https://sso.example.com") return;
  if (typeof event.data !== "string") return;

  localStorage.setItem("token", event.data);
});
```

写入完成后，发送端可以移除 `iframe`：

```js
iframe.remove();
```

## 5. postMessage 必须校验来源

资料原示例直接保存 `event.data`，这会接受任意来源发送的消息。正式代码必须校验：

- `event.origin` 是否为可信认证域；
- `event.source` 是否为预期窗口；
- `event.data` 是否符合约定结构；
- Token 是否短时、可撤销并限制受众。

发送时也不要使用任意目标 `"*"`：

```js
iframe.contentWindow.postMessage(
  { type: "SSO_TOKEN", token },
  "https://app1.com",
);
```

接收端按消息类型验证：

```js
window.addEventListener("message", (event) => {
  if (event.origin !== "https://sso.example.com") return;
  if (event.data?.type !== "SSO_TOKEN") return;

  localStorage.setItem("token", event.data.token);
});
```

## 6. Cookie 与 LocalStorage 方案对比

### Cookie 会话

优点：

- 浏览器自动随请求发送；
- 使用 `HttpOnly` 后 JavaScript 无法直接读取；
- 服务端会话更容易统一撤销。

注意事项：

- 需要配置 `SameSite`、`Secure`、Domain、Path；
- 自动发送 Cookie 的接口要防范 CSRF；
- 跨站 Cookie 会受到浏览器隐私策略影响。

### LocalStorage Token

优点：

- 前端可以明确读取和附加 Token；
- 不会自动随每个请求发送。

注意事项：

- JavaScript 可读取，页面发生 XSS 时 Token 风险更高；
- 跨域不能直接共享，需要通信或重新认证；
- 登出和撤销需要所有应用协调。

## 7. 单点登出

SSO 不只要解决登录，也要设计登出：

1. 清除认证中心会话；
2. 通知或引导各应用清除本地会话；
3. 服务器拒绝已经撤销或过期的凭据；
4. 避免某个应用退出后，其他应用仍长期保持有效状态。

只删除一个应用的 `localStorage`，并不等于整个 SSO 会话已经失效。

## 8. 总结

1. SSO 的核心是多个应用共同信任统一认证中心。
2. 同一主域的子域可以按 Domain 规则共享 Cookie。
3. 不同域名通常通过重定向和可验证的一次性凭据建立各自会话。
4. `iframe + postMessage + localStorage` 可以实现跨域传递，但必须严格校验 `origin`、消息结构和目标域。
5. 长期 Token 不应直接放在 URL 中，LocalStorage 也必须考虑 XSS 风险。
6. 完整方案还要包含凭据过期、撤销和单点登出。
