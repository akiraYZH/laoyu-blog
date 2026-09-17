BOM（Browser Object Model，浏览器对象模型）用来访问和操作浏览器窗口本身。它和 DOM 的关注点不同：DOM 面向网页文档，BOM 面向浏览器环境。

| 对比项 | DOM | BOM |
| --- | --- | --- |
| 全称 | Document Object Model | Browser Object Model |
| 操作对象 | HTML/XML 文档和节点 | 浏览器窗口及其能力 |
| 常见入口 | `document` | `window` |

BOM 的核心对象是 `window`，常见成员还包括 `location`、`navigator`、`screen` 和 `history`。

## 1. window

`window` 一方面代表当前浏览器窗口，另一方面也是传统浏览器脚本中的全局对象。

### 1.1 作为全局对象

在传统的非模块脚本中，顶层使用 `var` 声明的变量和函数可能成为 `window` 的属性：

```js
var name = "js";

function lookName() {
  alert(this.name);
}

console.log(window.name); // "js"
lookName(); // "js"（传统非严格脚本）
window.lookName(); // "js"
```

> 说明：`let`、`const` 和 ES Module 不遵循上述顶层 `var` 的同一绑定方式；严格模式下普通函数直接调用时，`this` 也不会自动指向 `window`。这段代码主要用于理解传统浏览器脚本。

### 1.2 移动、调整与滚动窗口

资料列出了以下窗口方法：

- `moveBy(x, y)`：相对当前位置移动窗口；
- `moveTo(x, y)`：移动到指定位置；
- `resizeBy(width, height)`：相对调整窗口大小；
- `resizeTo(width, height)`：调整为指定大小；
- `scrollTo(x, y)`：滚动到指定坐标；
- `scrollBy(x, y)`：按指定距离滚动。

```js
window.scrollTo(0, 500);
window.scrollBy(0, 200);
```

浏览器通常会限制脚本移动或调整普通用户窗口，但滚动方法在页面中仍很常见。

### 1.3 打开和关闭窗口

`window.open()` 可以打开 URL，并可指定目标窗口名称：

```js
window.open("https://www.example.com", "topFrame");
```

第二个参数和链接的 `target` 作用相近：

```html
<a href="https://www.example.com" target="topFrame">打开页面</a>
```

`window.open()` 会返回新窗口的引用：

```js
const myWin = window.open("https://www.example.com", "myWin");
```

通过脚本打开的窗口可以调用 `close()`：

```js
myWin?.close();
```

新窗口对象的 `opener` 属性会指向打开它的原窗口。实际项目中，如果新页面不需要访问原页面，应考虑使用 `noopener`，避免不必要的窗口控制关系。

## 2. location

`location` 描述当前页面 URL，也可以用于导航或重新加载页面。以这个 URL 为例：

```text
http://foouser:barpassword@www.wrox.com:80/WileyCDA/?q=javascript#contents
```

常见属性如下：

| 属性 | 示例 | 含义 |
| --- | --- | --- |
| `hash` | `#contents` | URL 中井号及后面的片段 |
| `host` | `www.wrox.com:80` | 主机名和端口 |
| `hostname` | `www.wrox.com` | 主机名，不含端口 |
| `href` | 完整 URL | 当前完整地址 |
| `pathname` | `/WileyCDA/` | 服务器路径 |
| `port` | `80` | 端口号，没有则为空字符串 |
| `protocol` | `http:` | 使用的协议 |
| `search` | `?q=javascript` | 查询字符串 |

```js
console.log(location.hostname);
console.log(location.pathname);
console.log(location.search);
console.log(location.hash);
```

修改 `location` 的多数属性会导致浏览器导航或重新加载新的 URL：

```js
location.hash = "details";
```

重新加载当前页面使用：

```js
location.reload();
```

> 说明：资料中的 `location.reload(true)` 曾被解释为“强制从服务器重新加载”，但这个布尔参数并不是现行标准的一部分，现代代码直接使用 `location.reload()`。

## 3. navigator

`navigator` 主要提供浏览器、设备和平台能力信息。资料列出的属性和方法很多，可以按用途理解。

### 3.1 浏览器和平台信息

```js
console.log(navigator.appName);
console.log(navigator.appVersion);
console.log(navigator.platform);
console.log(navigator.userAgent);
console.log(navigator.vendor);
```

其中还包括兼容性历史属性 `appCodeName`、`product`、`productSub`、`buildID` 和 `vendorSub`。这些信息可能被浏览器简化或伪装，不能仅凭 User-Agent 判断所有功能。

### 3.2 语言、网络与硬件

```js
console.log(navigator.language);
console.log(navigator.languages);
console.log(navigator.onLine);
console.log(navigator.hardwareConcurrency);
console.log(navigator.deviceMemory);
console.log(navigator.maxTouchPoints);
```

相关成员还包括 `connection`、`cookieEnabled`、`javaEnabled()`、`battery` 和 `vibrate()`。部分 API 可能已废弃、兼容性有限或需要权限。

### 3.3 媒体、位置与权限

```js
navigator.geolocation.getCurrentPosition((position) => {
  console.log(position.coords.latitude);
});

navigator.mediaDevices
  .getUserMedia({ audio: true })
  .then((stream) => console.log(stream));

navigator.permissions
  .query({ name: "geolocation" })
  .then((status) => console.log(status.state));
```

资料还列出了 `activeVRDisplays`、`getVRDisplays()`、`getUserMedia()`、`mediaCapabilities`、`mimeTypes`、`plugins` 和 `requestMediaKeySystemAccess()`。这些能力会受到 HTTPS、安全策略、用户授权和浏览器兼容性的限制。

### 3.4 其他平台能力

```js
navigator.sendBeacon("/analytics", JSON.stringify({ event: "leave" }));

navigator.share?.({
  title: "文章",
  url: location.href,
});
```

其他常见成员包括：

- `credentials`：凭据管理；
- `locks`：Web Locks；
- `serviceWorker`：Service Worker 容器；
- `storage`：存储管理；
- `registerProtocolHandler()`：注册协议处理器；
- `doNotTrack`：用户“不跟踪”设置；
- `oscpu`：操作系统或 CPU 信息；
- `webdriver`：浏览器是否受自动化程序控制。

> 说明：`navigator` 的属性并非每个浏览器都支持。正式代码应先检测能力是否存在，再决定是否调用。

## 4. screen

`screen` 保存客户端显示器信息，它描述的是浏览器窗口外部的屏幕，而不是网页内容区域。

| 属性 | 含义 |
| --- | --- |
| `availHeight`、`availWidth` | 扣除系统组件后的可用高度和宽度 |
| `availLeft`、`availTop` | 可用屏幕区域左侧和顶部位置 |
| `height`、`width` | 屏幕像素高度和宽度 |
| `colorDepth` | 显示颜色位数 |
| `pixelDepth` | 屏幕位深 |
| `left`、`top` | 当前屏幕相对位置 |
| `orientation` | 屏幕方向 |

```js
console.log(screen.width, screen.height);
console.log(screen.availWidth, screen.availHeight);
console.log(screen.orientation?.type);
```

## 5. history

`history` 用来操作当前标签页的浏览历史，可以向前、向后，或按指定步数跳转。

### 5.1 go

传入整数时，正数表示向前，负数表示向后：

```js
history.go(3); // 向前跳转三个记录
history.go(-1); // 向后跳转一个记录
```

资料还展示了传入字符串的形式：

```js
history.go("example.com");
```

> 说明：按字符串查找历史记录属于旧式行为，现代浏览器支持并不稳定，不应作为可靠导航方案。

### 5.2 forward、back 与 length

```js
history.forward(); // 前进一个页面
history.back(); // 后退一个页面
console.log(history.length); // 当前会话历史记录数量
```

## 6. 总结

- BOM 操作浏览器环境，DOM 操作文档节点。
- `window` 是 BOM 核心对象，也承担传统脚本中的全局对象角色。
- `location` 提供 URL 各组成部分并负责导航、刷新。
- `navigator` 提供浏览器、设备、网络、媒体和权限等能力信息。
- `screen` 描述客户端屏幕，`history` 操作当前标签页的会话历史。
- 许多 BOM API 受浏览器策略、权限和兼容性限制，调用前应做能力检测。
