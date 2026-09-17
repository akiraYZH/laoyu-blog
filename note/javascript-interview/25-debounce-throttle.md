`resize`、`scroll`、`keypress`、`mousemove` 等事件可能在短时间内连续触发。如果每次都执行昂贵逻辑，会造成大量重复计算。防抖（debounce）和节流（throttle）都用于限制函数执行频率，但行为不同。

## 1. 防抖与节流的区别

- 防抖：事件持续触发时不断重新计时，只有停止触发一段时间后才执行；
- 节流：事件持续触发时，保证一段时间内最多执行一次。

```text
连续触发：||||||||||||
防抖执行：            |
节流执行：|---|---|---|
```

常见场景：

- 搜索输入联想：防抖；
- 表单输入校验：防抖；
- 滚动位置计算：节流；
- 窗口尺寸变化处理：防抖或节流，取决于交互要求。

## 2. 时间戳节流

资料先使用时间戳实现节流：

```js
function throttled1(fn, delay = 500) {
  let oldTime = Date.now();

  return function (...args) {
    const newTime = Date.now();

    if (newTime - oldTime >= delay) {
      fn.apply(this, args);
      oldTime = newTime;
    }
  };
}
```

每次事件触发时比较当前时间与上次执行时间。间隔达到 `delay` 才执行函数。

这个版本第一次调用通常要等待一个完整间隔。如果希望第一次立即执行，可以把初始时间设为 `0`：

```js
let oldTime = 0;
```

## 3. 定时器节流

```js
function throttled2(fn, delay = 500) {
  let timer = null;

  return function (...args) {
    if (timer) return;

    timer = setTimeout(() => {
      fn.apply(this, args);
      timer = null;
    }, delay);
  };
}
```

第一次事件创建定时器。在定时器结束前，后续事件不会创建新任务；时间到后执行一次，并重新允许下一轮调度。

这个实现属于尾部执行：不会立即执行，而是在等待时间结束时执行。

## 4. 时间戳与定时器结合

资料还给出结合方案：到达间隔时立即执行，否则安排剩余时间后的尾部执行。

```js
function throttled(fn, delay) {
  let timer = null;
  let startTime = Date.now();

  return function (...args) {
    const currentTime = Date.now();
    const remaining = delay - (currentTime - startTime);
    const context = this;

    clearTimeout(timer);

    if (remaining <= 0) {
      fn.apply(context, args);
      startTime = Date.now();
    } else {
      timer = setTimeout(() => {
        fn.apply(context, args);
        startTime = Date.now();
      }, remaining);
    }
  };
}
```

这里必须在定时器中通过 `apply()` 调用包装函数，才能保留当次的 `this` 和参数。直接写 `setTimeout(fn, remaining)` 会丢失它们。

## 5. 基础防抖

```js
function debounce(func, wait) {
  let timeout;

  return function (...args) {
    const context = this;

    clearTimeout(timeout);

    timeout = setTimeout(function () {
      func.apply(context, args);
    }, wait);
  };
}
```

每次调用包装函数都会先取消旧定时器，再重新计时。只有事件停止触发并经过 `wait`，原函数才会执行。

```js
const search = debounce((keyword) => {
  console.log("搜索", keyword);
}, 300);

input.addEventListener("input", (event) => {
  search(event.target.value);
});
```

## 6. 立即执行防抖

有时希望第一次触发立即执行，之后连续触发都被忽略，直到等待期结束：

```js
function debounce(func, wait, immediate = false) {
  let timeout = null;

  return function (...args) {
    const context = this;
    const callNow = immediate && timeout === null;

    clearTimeout(timeout);

    timeout = setTimeout(function () {
      timeout = null;

      if (!immediate) {
        func.apply(context, args);
      }
    }, wait);

    if (callNow) {
      func.apply(context, args);
    }
  };
}
```

`timeout` 不仅保存定时器，也标记当前是否仍在等待窗口内。等待结束后必须重新设为 `null`，下一轮第一次触发才能再次立即执行。

## 7. 为什么要保留 this 和参数

包装函数最终可能作为对象方法或事件处理程序调用：

```js
const handler = {
  name: "scroll handler",
  run: debounce(function (event) {
    console.log(this.name, event.type);
  }, 200),
};
```

通过 `func.apply(context, args)` 执行，原函数才能取得调用时的 `this` 和事件参数。

## 8. 总结

1. 防抖等待连续触发停止后执行，节流限制固定时间内的最大执行次数。
2. 时间戳节流更偏向立即执行，定时器节流更偏向尾部执行。
3. 防抖的关键是每次触发先 `clearTimeout()`，再重新设置定时器。
4. 立即执行防抖需要用定时器状态控制等待窗口。
5. 封装时要通过 `apply()` 或等价方式保留 `this` 和参数。
