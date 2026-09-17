内存泄漏是指程序不再需要某块内存，但它仍然被可达引用保留，垃圾回收器无法释放。JavaScript 有自动垃圾回收，不代表代码绝不会泄漏。

## 1. 手动管理与自动回收

在 C 语言中，开发者需要手动申请和释放内存：

```c
char *buffer;
buffer = (char *)malloc(42);

// 使用 buffer

free(buffer);
```

JavaScript 引擎会自动追踪对象是否仍然可达，并回收不可达对象。开发者不能直接调用 `free()`，但可以解除不再需要的引用。

## 2. 局部变量的生命周期

```js
var m = 0;
var n = 19;

add(m, n);
console.log(n);

function add(a, b) {
  a++;
  var c = a + b;
  return c;
}
```

函数执行时创建参数 `a`、`b` 和局部变量 `c`。函数结束后，如果没有闭包继续引用它们，对应环境可以被回收。全局变量 `m`、`n` 则会随全局环境持续存在。

## 3. 解除对象引用

```js
let arr = [1, 2, 3, 4];

console.log("hello world");
```

变量 `arr` 指向数组，因此数组仍然可达。如果确认不再使用，可以解除引用：

```js
arr = null;
```

这不是立刻命令引擎释放内存，而是让变量不再指向原数组。当没有其他引用时，数组之后可以被垃圾回收。

## 4. 意外的全局变量

### 4.1 漏写声明

资料中的示例：

```js
function foo(arg) {
  bar = "this is a hidden global variable";
}
```

非严格脚本中，漏写 `let`、`const` 或 `var` 可能创建全局变量 `bar`。全局变量会持续到页面关闭或主动清除。

严格模式可以让这种错误直接抛出：

```js
"use strict";

function foo() {
  bar = "value"; // ReferenceError
}
```

### 4.2 普通调用中误用 this

```js
function foo() {
  this.variable = "potential accidental global";
}

foo();
```

传统非严格浏览器脚本中，`this` 可能是 `window`，于是 `variable` 变成全局属性。严格模式下 `this` 为 `undefined`，能更早暴露错误。

## 5. 没有清理的定时器

```js
const someResource = getData();

const timer = setInterval(function () {
  const node = document.getElementById("Node");

  if (node) {
    node.innerHTML = JSON.stringify(someResource);
  }
}, 1000);
```

只要定时器继续存在，回调函数就会被引用；回调又引用 `someResource`，因此资源无法回收。即使 DOM 节点已经删除，定时器仍会持续运行。

不再需要时清理：

```js
clearInterval(timer);
```

组件销毁、页面切换或业务任务完成时，都应停止对应的 `setInterval`、长延迟 `setTimeout` 和轮询任务。

## 6. 闭包保留无用对象

资料给出的例子：

```js
function bindEvent() {
  var obj = document.createElement("div");

  var unused = function () {
    console.log(obj, "闭包仍然引用 obj");
  };

  obj = null;
}
```

闭包是否造成内存保留，要看它本身是否仍被外部引用。仅仅存在闭包并不自动构成泄漏；但如果闭包被全局变量、事件监听器或定时器长期保存，它引用的大对象也会一起存活。

## 7. 脱离 DOM 的节点

从页面删除节点，只是断开 DOM 树的引用。如果 JavaScript 变量仍然引用它，节点不会变成不可达：

```js
let refA = document.getElementById("refA");

document.body.removeChild(refA);
console.log(refA); // 变量仍然能访问这个 div

refA = null;
```

如果节点或其子节点上还有事件监听器、观察器、第三方库缓存，也要按对应生命周期清理。

## 8. 没有移除的事件监听器

```js
function handleResize() {
  console.log(window.innerWidth);
}

window.addEventListener("resize", handleResize);
```

不再需要时，使用同一个函数引用移除：

```js
window.removeEventListener("resize", handleResize);
```

匿名函数如果没有保存引用，就难以准确移除：

```js
window.addEventListener("resize", () => {
  console.log(window.innerWidth);
});
```

现代 API 也可以使用 `AbortController` 统一结束监听：

```js
const controller = new AbortController();

window.addEventListener("resize", handleResize, {
  signal: controller.signal,
});

controller.abort();
```

## 9. 常见排查方向

1. 全局变量和全局缓存是否持续增长；
2. 定时器、轮询和异步订阅是否在生命周期结束时停止；
3. 事件监听器是否正确移除；
4. 已删除的 DOM 节点是否仍被变量或缓存引用；
5. 闭包是否保留了不再需要的大对象；
6. Map、数组或对象缓存是否有上限和清理策略。

浏览器开发者工具的 Memory 面板可以通过 Heap Snapshot 对比对象数量和引用路径；Performance 面板可以观察长时间运行时的内存趋势。

## 10. 总结

1. 垃圾回收器只能释放不可达对象，仍有引用就不会回收。
2. 漏写声明和普通函数中的错误 `this` 可能创建意外全局变量。
3. 未停止的定时器会持续保留回调及其引用的数据。
4. 从 DOM 树移除节点后，JavaScript 引用仍可能保留它。
5. 事件监听器、订阅和观察器要在生命周期结束时清理。
6. 闭包不是天然泄漏，真正问题是闭包是否被长期引用并保留无用数据。
