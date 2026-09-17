判断元素是否进入可视区域，常见方式有两类：手动计算元素与视口的位置关系，以及使用 `IntersectionObserver` 让浏览器异步报告相交状态。

## 1. 先理解尺寸与滚动属性

### 1.1 offset

`offsetTop` 表示元素相对其 `offsetParent` 顶部的位置。它不一定直接等于元素到整个文档顶部的距离，因为中间可能存在多个定位祖先。

### 1.2 client

- `clientWidth`：内容宽度加左右内边距；
- `clientHeight`：内容高度加上下内边距。

它们不包含边框和滚动条。

### 1.3 scroll

- `scrollWidth`、`scrollHeight`：元素全部可滚动内容的宽高；
- `scrollLeft`、`scrollTop`：当前水平和垂直滚动距离。

页面向下滚动后，`scrollTop` 通常大于 `0`。

## 2. 使用 offsetTop 与 scrollTop

资料给出的基本判断是：

```js
element.offsetTop - document.documentElement.scrollTop <= viewportHeight;
```

封装后：

```js
function isInViewPortOfOne(element) {
  const viewportHeight =
    window.innerHeight ||
    document.documentElement.clientHeight ||
    document.body.clientHeight;

  const offsetTop = element.offsetTop;
  const scrollTop = document.documentElement.scrollTop;
  const top = offsetTop - scrollTop;

  return top <= viewportHeight;
}
```

这段代码判断元素顶部是否已经到达视口底部以内。

它的局限是：`offsetTop` 相对 `offsetParent`，复杂布局下不能简单当作文档绝对位置；而且只检查了顶部，没有确认元素是否已经滚出视口上方或是否位于水平范围内。

## 3. getBoundingClientRect

`getBoundingClientRect()` 返回元素相对当前视口的矩形：

```js
const target = document.querySelector(".target");
const clientRect = target.getBoundingClientRect();

console.log(clientRect);
```

返回结果包含：

```text
top、right、bottom、left
x、y、width、height
```

例如：

```js
{
  top: 162.625,
  right: 1017,
  bottom: 556.21875,
  left: 333,
  width: 684,
  height: 393.59375,
}
```

## 4. 判断元素完全进入视口

```js
function isInViewPort(element) {
  const viewWidth =
    window.innerWidth || document.documentElement.clientWidth;
  const viewHeight =
    window.innerHeight || document.documentElement.clientHeight;

  const { top, right, bottom, left } =
    element.getBoundingClientRect();

  return (
    top >= 0 &&
    left >= 0 &&
    right <= viewWidth &&
    bottom <= viewHeight
  );
}
```

四个条件同时满足，表示元素完整处于视口内。

如果只要求有一部分进入视口，可以改为判断矩形是否有重叠：

```js
function isPartlyInViewPort(element) {
  const viewWidth = window.innerWidth;
  const viewHeight = window.innerHeight;
  const { top, right, bottom, left } =
    element.getBoundingClientRect();

  return (
    bottom > 0 &&
    right > 0 &&
    top < viewHeight &&
    left < viewWidth
  );
}
```

## 5. 滚动事件方案

资料创建大量 `.target` 元素，并在滚动时逐个调用 `getBoundingClientRect()`：

```html
<div class="container"></div>
```

```css
.container {
  display: flex;
  flex-wrap: wrap;
}

.target {
  margin: 5px;
  width: 20px;
  height: 20px;
  background: red;
}
```

```js
const container = document.querySelector(".container");

function createTargets() {
  container.innerHTML = new Array(100000)
    .fill('<div class="target"></div>')
    .join("");
}
```

```js
const targets = document.querySelectorAll(".target");

window.addEventListener("scroll", () => {
  targets.forEach((element) => {
    if (isInViewPort(element)) {
      element.style.backgroundColor = "yellow";
    }
  });
});
```

这种方式每次滚动都遍历全部元素并读取布局信息，元素很多时成本较高。至少应该配合节流，并避免在同一循环中反复交错读写布局。

## 6. IntersectionObserver

`IntersectionObserver` 可以异步观察目标元素与视口或滚动容器的相交情况，不需要自己在每次滚动时计算位置。

```js
const options = {
  threshold: 1.0,
  root: document.querySelector("#scrollArea"),
};

const observer = new IntersectionObserver(callback, options);
```

- `root`：作为观察范围的滚动容器；省略或设为 `null` 时使用视口；
- `threshold`：相交比例达到多少时触发，范围为 `0` 到 `1`；
- `1.0` 表示目标完整进入观察区域时触发。

## 7. 回调参数

```js
function callback(entries, observer) {
  entries.forEach((entry) => {
    console.log(entry.time);
    console.log(entry.rootBounds);
    console.log(entry.boundingClientRect);
    console.log(entry.intersectionRect);
    console.log(entry.intersectionRatio);
    console.log(entry.target);
  });
}
```

其中：

- `boundingClientRect`：目标元素矩形；
- `rootBounds`：观察根区域矩形；
- `intersectionRect`：两者重叠部分；
- `intersectionRatio`：目标可见比例；
- `target`：被观察元素。

开始观察：

```js
const target = document.querySelector(".target");
observer.observe(target);
```

不再需要时停止：

```js
observer.unobserve(target);
observer.disconnect();
```

## 8. 使用 Observer 批量变色

```js
const observer = new IntersectionObserver(getYellow, {
  threshold: 1.0,
});

function getYellow(entries) {
  entries.forEach((entry) => {
    if (entry.isIntersecting) {
      entry.target.style.backgroundColor = "yellow";
    }
  });
}

document.querySelectorAll(".target").forEach((element) => {
  observer.observe(element);
});
```

相比滚动时遍历 100000 个元素，浏览器可以更有效地安排相交检测。

## 9. 总结

1. `offsetTop - scrollTop` 可以做简单纵向判断，但复杂布局下有局限。
2. `getBoundingClientRect()` 返回元素相对视口的位置和大小。
3. 检查四条边可判断完全可见；检查矩形重叠可判断部分可见。
4. 滚动监听加批量位置计算可能产生性能问题。
5. 大量元素、懒加载或曝光统计优先考虑 `IntersectionObserver`。
