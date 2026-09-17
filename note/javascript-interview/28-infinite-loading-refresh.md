上拉加载用于用户接近列表底部时请求下一页数据；下拉刷新用于用户在列表顶部继续向下拖动时重新加载第一页。两者都需要正确判断位置、控制重复请求，并在 DOM 更新后恢复滚动状态。

资料同时介绍了原生实现思路，以及 `iScroll`、`better-scroll`、`pulltorefresh.js` 等库。

## 1. 上拉加载的滚动判断

页面滚动相关的三个关键值是：

- `scrollTop`：已经向上滚动的距离；
- `clientHeight`：当前视口高度；
- `scrollHeight`：整个可滚动内容高度。

到达底部时大致满足：

```js
scrollTop + clientHeight >= scrollHeight;
```

为了提前加载，可以预留距离：

```js
const clientHeight = document.documentElement.clientHeight;
const scrollHeight = document.body.scrollHeight;
const scrollTop = document.documentElement.scrollTop;
const distance = 50;

if (scrollTop + clientHeight >= scrollHeight - distance) {
  console.log("接近底部，加载下一页");
}
```

滚动事件会高频触发，实际代码应配合节流，并增加请求锁：

```js
let loading = false;
let hasMore = true;

async function loadMore() {
  if (loading || !hasMore) return;

  loading = true;

  try {
    const result = await requestNextPage();
    hasMore = result.length === pageSize;
  } finally {
    loading = false;
  }
}
```

## 2. 原生下拉刷新原理

移动端触摸事件流程：

1. `touchstart`：记录手指开始位置 `e.touches[0].pageY`；
2. `touchmove`：计算移动距离，让内容通过 `translateY` 下移；
3. 到达阈值后提示可以松手刷新；
4. `touchend`：执行刷新回调，并把内容复位到 `translateY(0)`。

## 3. 页面结构

```html
<main>
  <p class="refreshText"></p>

  <ul id="refreshContainer">
    <li>111</li>
    <li>222</li>
    <li>333</li>
    <li>444</li>
    <li>555</li>
  </ul>
</main>
```

## 4. touchstart

```js
const element = document.getElementById("refreshContainer");
const refreshText = document.querySelector(".refreshText");

let startPosition = 0;
let transitionHeight = 0;

element.addEventListener(
  "touchstart",
  function (event) {
    startPosition = event.touches[0].pageY;
    element.style.position = "relative";
    element.style.transition = "transform 0s";
  },
  false,
);
```

只有列表已经位于顶部时，才应该启动下拉刷新：

```js
if (window.scrollY > 0) return;
```

## 5. touchmove

```js
element.addEventListener(
  "touchmove",
  function (event) {
    transitionHeight = event.touches[0].pageY - startPosition;

    if (transitionHeight > 0 && transitionHeight < 60) {
      refreshText.innerText = "下拉刷新";
      element.style.transform = `translateY(${transitionHeight}px)`;

      if (transitionHeight > 55) {
        refreshText.innerText = "释放更新";
      }
    }
  },
  false,
);
```

代码只处理向下移动，并把最大移动距离限制在 `60px`。当距离超过 `55px` 时，提示从“下拉刷新”变成“释放更新”。

## 6. touchend

```js
element.addEventListener(
  "touchend",
  async function () {
    element.style.transition = "transform 0.5s ease";
    element.style.transform = "translateY(0px)";

    if (transitionHeight > 55) {
      refreshText.innerText = "正在刷新...";
      await reloadFirstPage();
    }

    refreshText.innerText = "";
    transitionHeight = 0;
  },
  false,
);
```

资料原示例在松手后直接进入刷新状态；这里补上阈值判断，避免轻微触摸也发起请求。

## 7. 使用 better-scroll

页面结构需要一个固定高度的 wrapper，内部只能有一个直接子容器：

```html
<div id="position-wrapper">
  <div>
    <p class="refresh">下拉刷新</p>
    <div class="position-list"></div>
    <p class="more">上拉加载更多</p>
  </div>
</div>
```

安装并注册插件：

```js
import BScroll from "@better-scroll/core";
import PullDown from "@better-scroll/pull-down";
import PullUp from "@better-scroll/pull-up";

BScroll.use(PullDown);
BScroll.use(PullUp);
```

## 8. 初始化与事件绑定

```js
let pageNo = 1;
const pageSize = 10;
let dataList = [];
let hasMore = true;

const scroll = new BScroll("#position-wrapper", {
  scrollY: true,
  click: true,
  pullUpLoad: true,
  pullDownRefresh: {
    threshold: 50,
    stop: 0,
  },
});

scroll.on("pullingDown", pullingDownHandler);
scroll.on("scroll", scrollHandler);
scroll.on("pullingUp", pullingUpHandler);
```

`click: true` 让滚动容器中的普通点击仍能正常触发。`threshold` 表示下拉超过多少距离触发刷新。

## 9. 下拉刷新处理

```js
async function pullingDownHandler() {
  dataList = [];
  pageNo = 1;
  hasMore = true;

  document.querySelector(".more").textContent = "上拉加载更多";

  await getList();

  scroll.finishPullDown();
  scroll.refresh();
}
```

刷新时清空旧列表并回到第一页。请求和 DOM 更新结束后调用：

- `finishPullDown()`：告诉插件本次下拉已经完成；
- `refresh()`：重新计算滚动区域尺寸。

## 10. 上拉加载处理

```js
async function pullingUpHandler() {
  if (!hasMore) {
    document.querySelector(".more").textContent = "没有更多数据";
    scroll.finishPullUp();
    return;
  }

  pageNo++;
  await getList();

  scroll.finishPullUp();
  scroll.refresh();
}
```

资料中的 `await this.getlist()` 没有必要依赖 `this`；直接调用 `getList()` 更清楚。

## 11. 获取并追加数据

```js
async function getList() {
  const result = await fetchPage(pageNo, pageSize);

  dataList = dataList.concat(result);

  if (result.length < pageSize) {
    hasMore = false;
  }

  renderList(dataList);
}
```

列表 DOM 更新后必须调用 `scroll.refresh()`，否则插件仍使用旧高度。

## 12. 总结

1. 上拉加载通过 `scrollTop + clientHeight` 与 `scrollHeight` 判断是否接近底部。
2. 下拉刷新通过 `touchstart`、`touchmove`、`touchend` 记录距离、移动内容并触发刷新。
3. 原生实现需要阈值、请求锁、错误恢复和滚动节流。
4. better-scroll 通过 `pullingDown`、`pullingUp` 事件封装交互。
5. 每次数据和 DOM 更新后，要调用完成方法并重新计算滚动区域。
