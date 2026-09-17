JavaScript 在浏览器中通常只有一个主线程负责执行 JavaScript 和更新页面。网络请求、定时器等耗时工作由浏览器提供的能力处理；任务完成后，对应回调进入队列，等待主线程空闲。协调这些工作的机制就是事件循环（Event Loop）。

## 1. 同步任务与异步任务

同步代码按照执行栈顺序立即执行：

```js
console.log(1);
console.log(2);
console.log(3);
```

输出：

```text
1
2
3
```

定时器、Ajax、事件回调等异步任务不会在注册时立即执行回调：

```js
console.log(1);

setTimeout(() => {
  console.log(2);
}, 0);

console.log(3);
```

输出为：

```text
1
3
2
```

即使延迟是 `0`，回调也要等当前同步代码执行完，并在队列中等待事件循环调度。

## 2. 执行栈、浏览器能力和任务队列

可以把浏览器运行过程简化为：

```text
同步代码 → 执行栈
定时器/网络/事件 → 浏览器处理
完成后的回调 → 对应任务队列
执行栈清空 → 事件循环取出队列任务
```

事件循环会不断检查执行栈是否为空。只有当前任务的同步代码结束后，队列中的回调才有机会进入执行栈。

## 3. 宏任务与微任务

异步回调还可以分为宏任务和微任务。

常见宏任务包括：

- 整段脚本；
- `setTimeout`、`setInterval` 回调；
- 用户事件回调；
- 部分 I/O 回调。

常见微任务包括：

- `Promise.then()`、`catch()`、`finally()`；
- `queueMicrotask()`；
- `await` 后续代码。

一轮常见执行顺序可以理解为：

```text
执行一个宏任务
  ↓
清空本轮产生的全部微任务
  ↓
浏览器可能进行渲染
  ↓
执行下一个宏任务
```

## 4. Promise 与 setTimeout 的顺序

```js
console.log(1);

setTimeout(() => {
  console.log(2);
}, 0);

new Promise((resolve) => {
  console.log("new Promise");
  resolve();
}).then(() => {
  console.log("then");
});

console.log(3);
```

输出：

```text
1
new Promise
3
then
2
```

逐步分析：

1. `console.log(1)` 是同步代码，立即输出 `1`；
2. `setTimeout` 注册定时器，回调之后进入宏任务队列；
3. `new Promise` 的执行器函数会同步执行，所以输出 `new Promise`；
4. `resolve()` 让 `.then()` 回调进入微任务队列；
5. 同步执行 `console.log(3)`；
6. 当前脚本结束后先清空微任务，输出 `then`；
7. 再执行定时器宏任务，输出 `2`。

## 5. async 函数

`async` 函数总是返回 Promise。下面两个函数在返回结果这一点上相近：

```js
function f() {
  return Promise.resolve("TEST");
}

async function asyncF() {
  return "TEST";
}
```

调用 `asyncF()` 得到的是一个兑现值为 `"TEST"` 的 Promise：

```js
asyncF().then((value) => console.log(value)); // "TEST"
```

## 6. await

`await` 会等待右侧值对应的 Promise 状态确定。即使右侧是普通值，也会按已兑现 Promise 的方式处理：

```js
async function f() {
  return await 123;
}

f().then((value) => console.log(value)); // 123
```

更重要的是：`await` 之前的代码同步执行，`await` 之后的代码进入微任务继续执行。

```js
async function fn1() {
  console.log(1);
  await fn2();
  console.log(2);
}

async function fn2() {
  console.log("fn2");
}

fn1();
console.log(3);
```

输出：

```text
1
fn2
3
2
```

执行 `fn1()` 时先输出 `1`，调用 `fn2()` 输出 `fn2`；遇到 `await` 后，`fn1` 剩余部分延后到微任务。全局同步代码先输出 `3`，然后微任务输出 `2`。

## 7. 综合题

```js
async function async1() {
  console.log("async1 start");
  await async2();
  console.log("async1 end");
}

async function async2() {
  console.log("async2");
}

console.log("script start");

setTimeout(function () {
  console.log("settimeout");
});

async1();

new Promise(function (resolve) {
  console.log("promise1");
  resolve();
}).then(function () {
  console.log("promise2");
});

console.log("script end");
```

输出顺序：

```text
script start
async1 start
async2
promise1
script end
async1 end
promise2
settimeout
```

分析队列形成顺序：

1. 整段脚本作为宏任务开始，输出 `script start`；
2. 定时器回调登记为后续宏任务；
3. `async1()` 同步执行，输出 `async1 start`；
4. 调用 `async2()`，同步输出 `async2`；
5. `await` 后的 `async1 end` 加入微任务队列；
6. Promise 执行器同步输出 `promise1`，其 `.then()` 加入微任务队列；
7. 输出 `script end`，同步代码结束；
8. 按进入顺序执行微任务：`async1 end`、`promise2`；
9. 最后执行定时器宏任务，输出 `settimeout`。

## 8. 总结

1. JavaScript 同步代码在执行栈中按顺序执行。
2. 异步能力完成后把回调放入对应队列，不会打断当前同步代码。
3. 当前宏任务结束后，会先清空微任务，再进入下一个宏任务。
4. Promise 执行器同步执行，`.then()` 回调属于微任务。
5. `async` 函数返回 Promise，`await` 之前同步执行，之后的代码以微任务继续。
6. 分析输出题时，先写同步输出，再列微任务，最后列宏任务。
