JavaScript 事件模型描述浏览器中的事件如何产生、如何沿 DOM 树传播，以及代码可以用哪些方式注册和移除事件处理程序。

## 1. 事件冒泡

页面中点击一个元素时，事件不仅可以在目标元素上触发，还会继续向外层祖先传播。

```html
<!doctype html>
<html lang="zh-CN">
  <head>
    <meta charset="UTF-8" />
    <title>Event Bubbling</title>
  </head>
  <body>
    <button id="clickMe">Click Me</button>
  </body>
</html>
```

```js
const button = document.getElementById("clickMe");

button.onclick = function () {
  console.log("1.button");
};

document.body.onclick = function () {
  console.log("2.body");
};

document.onclick = function () {
  console.log("3.document");
};

window.onclick = function () {
  console.log("4.window");
};
```

点击按钮后，资料中的输出顺序是：

```text
1.button
2.body
3.document
4.window
```

事件从具体目标 `button` 开始，逐层向 `body`、`document` 和 `window` 冒泡。

## 2. 事件处理程序的三种模型

浏览器中的事件绑定通常分为 HTML 事件处理程序、DOM0 级事件处理程序和 DOM2 级事件处理程序。资料还介绍了旧版 IE 的专有模型。

## 3. HTML 事件处理程序

HTML 事件处理程序直接写在标签属性中：

```html
<input type="button" onclick="fun()" value="点击" />
```

```js
function fun() {
  console.log("clicked");
}
```

这种写法把结构和 JavaScript 行为写在一起，不利于维护；现代项目通常使用 `addEventListener()`。

## 4. DOM0 级事件处理程序

DOM0 方式把函数赋给元素的 `onclick` 等属性：

```js
const btn = document.getElementById("btn");

btn.onclick = function fun() {
  console.log("clicked");
};
```

同一个事件属性只能保留最后一次赋值：

```js
btn.onclick = function fun1() {
  console.log("fun1");
};

btn.onclick = function fun2() {
  console.log("fun2");
};

// 点击时只执行 fun2
```

移除 DOM0 处理程序，只需把属性设为 `null`：

```js
btn.onclick = null;
```

## 5. DOM2 级事件处理程序

DOM2 使用 `addEventListener()` 和 `removeEventListener()`：

```js
element.addEventListener(eventType, handler, useCapture);
element.removeEventListener(eventType, handler, useCapture);
```

三个参数分别表示：

- `eventType`：事件名称，例如 `"click"`；
- `handler`：事件处理函数；
- `useCapture`：是否在捕获阶段处理，默认通常为 `false`。

```js
function showMessage() {
  console.log("clicked");
}

btn.addEventListener("click", showMessage, false);
btn.removeEventListener("click", showMessage, false);
```

移除时必须使用同一个函数引用和相同的捕获设置。下面这种匿名函数无法用另一个新匿名函数准确移除：

```js
btn.addEventListener("click", () => console.log("clicked"));
```

DOM2 允许同一元素的同一事件添加多个处理程序：

```js
btn.addEventListener("click", showMessage1, false);
btn.addEventListener("click", showMessage2, false);
btn.addEventListener("click", showMessage3, false);
```

## 6. 捕获、目标与冒泡

标准事件传播可以分为三个阶段：

```text
捕获阶段：外层 → 目标
目标阶段：到达被操作的元素
冒泡阶段：目标 → 外层
```

示例结构：

```html
<div id="div">
  <p id="p">
    <span id="span">Click Me!</span>
  </p>
</div>
```

```js
const div = document.getElementById("div");
const p = document.getElementById("p");

function onClickFn(event) {
  const tagName = event.currentTarget.tagName;
  const phase = event.eventPhase;
  console.log(tagName, phase);
}

div.addEventListener("click", onClickFn, false);
p.addEventListener("click", onClickFn, false);
```

点击 `span` 时，两个监听器都在冒泡阶段执行：

```text
P 3
DIV 3
```

`event.eventPhase` 的常见值是：

```text
1：捕获阶段
2：目标阶段
3：冒泡阶段
```

如果把第三个参数改为 `true`：

```js
div.addEventListener("click", onClickFn, true);
p.addEventListener("click", onClickFn, true);
```

输出顺序变成从外向内：

```text
DIV 1
P 1
```

## 7. target 与 currentTarget

在事件传播过程中：

- `event.target` 是最初触发事件的元素；
- `event.currentTarget` 是当前正在执行监听器的元素。

```js
div.addEventListener("click", (event) => {
  console.log(event.target.id); // 可能是 span
  console.log(event.currentTarget.id); // div
});
```

理解这个区别，是后续实现事件委托的基础。

## 8. 旧版 IE 事件模型

资料还列出了旧版 IE 的专有方法：

```js
element.attachEvent(eventType, handler);
element.detachEvent(eventType, handler);
```

```js
function showMessage() {
  console.log("clicked");
}

btn.attachEvent("onclick", showMessage);
btn.detachEvent("onclick", showMessage);
```

`attachEvent()`、`detachEvent()` 只用于很旧的 IE，现代浏览器代码使用标准的 `addEventListener()` 和 `removeEventListener()`。

## 9. 总结

1. 事件会经历捕获、目标和冒泡阶段。
2. HTML 属性事件直接写在标签中，DOM0 把函数赋给 `onclick` 等属性。
3. DOM0 同一事件只能保存一个属性处理程序，设为 `null` 即可移除。
4. DOM2 的 `addEventListener()` 可以注册多个监听器，并能选择捕获或冒泡阶段。
5. `event.target` 表示事件原始目标，`event.currentTarget` 表示当前监听器所在元素。
6. `attachEvent()` 属于旧 IE 专有 API，现代代码不再使用。
