闭包是指函数能够记住并访问它定义时所在的词法作用域，即使这个函数后来在外层作用域之外执行，仍然可以访问原来的变量。

## 1. 从嵌套函数开始

```js
function init() {
  var name = "Mozilla";

  function displayName() {
    alert(name);
  }

  displayName();
}

init();
```

`displayName()` 自己没有变量 `name`，但它定义在 `init()` 内部，因此可以沿词法作用域向外找到 `init` 中的 `name`。

如果内部函数只是立刻执行，这仍然体现词法作用域。闭包更典型的表现是：内部函数被返回或传到外部以后，依然能够使用外层变量。

## 2. 返回函数形成闭包

```js
function createGreeting(name) {
  return function () {
    return `Hello, ${name}`;
  };
}

const greetTom = createGreeting("Tom");

console.log(greetTom()); // "Hello, Tom"
```

`createGreeting()` 已经执行结束，但返回的函数仍然引用参数 `name`，所以相关词法环境会继续保留。

关系可以表示为：

```text
greetTom
  ↓ 引用返回的函数
返回的函数
  ↓ 通过闭包访问
name = "Tom"
```

## 3. 使用闭包保存配置

资料给出了调整页面字体大小的例子：

```js
function makeSizer(size) {
  return function () {
    document.body.style.fontSize = `${size}px`;
  };
}

const size12 = makeSizer(12);
const size14 = makeSizer(14);
const size16 = makeSizer(16);

document.getElementById("size-12").onclick = size12;
document.getElementById("size-14").onclick = size14;
document.getElementById("size-16").onclick = size16;
```

三次调用 `makeSizer()` 会创建三个不同的函数执行环境：

```text
size12 记住 size = 12
size14 记住 size = 14
size16 记住 size = 16
```

以后点击按钮时，对应函数会读取自己保存的 `size`，不会互相覆盖。

## 4. 闭包与函数柯里化

如果很多计算共享一个参数，可以先用闭包保存它，再传入剩余参数。

普通写法：

```js
function getArea(width, height) {
  return width * height;
}

const area1 = getArea(10, 20);
const area2 = getArea(10, 30);
const area3 = getArea(10, 40);
```

使用闭包拆分参数：

```js
function getArea(width) {
  return function (height) {
    return width * height;
  };
}

const getTenWidthArea = getArea(10);

const area1 = getTenWidthArea(20);
const area2 = getTenWidthArea(30);
const area3 = getTenWidthArea(40);

const getTwentyWidthArea = getArea(20);
```

`getTenWidthArea` 保存了 `width = 10`，后续只需要提供高度。这种把多参数调用逐步转换为单参数调用的写法，与函数柯里化思想相关。

## 5. 使用闭包实现私有状态

闭包可以让变量不直接暴露到外部，只通过指定函数读写。

```js
function makeCounter() {
  let privateCounter = 0;

  function changeBy(value) {
    privateCounter += value;
  }

  return {
    increment() {
      changeBy(1);
    },
    decrement() {
      changeBy(-1);
    },
    value() {
      return privateCounter;
    },
  };
}
```

```js
const counter1 = makeCounter();
const counter2 = makeCounter();

console.log(counter1.value()); // 0

counter1.increment();
counter1.increment();
console.log(counter1.value()); // 2

counter1.decrement();
console.log(counter1.value()); // 1

console.log(counter2.value()); // 0
```

`counter1` 和 `counter2` 来自两次独立调用，因此各自拥有不同的 `privateCounter`。外部不能直接访问这个变量，只能通过 `increment()`、`decrement()` 和 `value()` 操作。

也可以立即调用函数，只创建一个计数器对象：

```js
const Counter = (function () {
  let privateCounter = 0;

  function changeBy(value) {
    privateCounter += value;
  }

  return {
    increment() {
      changeBy(1);
    },
    decrement() {
      changeBy(-1);
    },
    value() {
      return privateCounter;
    },
  };
})();
```

## 6. 闭包为什么能保存变量

JavaScript 使用词法作用域。内部函数创建时，会关联它定义位置的外层环境。当内部函数继续被外部变量、事件监听器或返回值引用时，它仍需要访问这些外层变量，因此相关环境不能立刻被回收。

```js
function outer() {
  const value = 10;

  return function inner() {
    return value;
  };
}

const inner = outer();
```

这里不是把 `value` 复制进函数代码，而是返回的 `inner` 保留了访问外层词法环境的能力。

## 7. 闭包与内存

闭包本身不是内存泄漏。只有当某个闭包被长期引用，并且它又保留了不再需要的大量数据时，才可能增加内存占用。

```js
function createHandler() {
  const largeData = new Array(100000).fill("data");

  return function handler() {
    console.log(largeData.length);
  };
}

let handler = createHandler();

// 不再需要时解除外部引用
handler = null;
```

事件监听器、定时器和全局变量都可能让闭包长期存活。是否需要清理，应看外部是否仍然保留引用。

## 8. 构造函数中重复创建方法

资料还提醒：如果每个实例都不需要独立的函数闭包，就不要在构造函数中重复创建相同方法。

每个实例都创建新函数：

```js
function MyObject(name, message) {
  this.name = name.toString();
  this.message = message.toString();

  this.getName = function () {
    return this.name;
  };

  this.getMessage = function () {
    return this.message;
  };
}
```

把共享方法放到原型：

```js
function MyObject(name, message) {
  this.name = name.toString();
  this.message = message.toString();
}

MyObject.prototype.getName = function () {
  return this.name;
};

MyObject.prototype.getMessage = function () {
  return this.message;
};
```

第二种写法让所有实例共享同一组方法。只有确实需要让每个实例的方法保存独立私有变量时，才有必要让构造函数每次创建闭包。

## 9. 总结

1. 闭包让函数能够继续访问定义位置的外层词法环境。
2. 返回函数、事件回调和函数工厂都经常形成闭包。
3. 每次调用外层函数都会创建独立环境，因此可以保存不同配置或私有状态。
4. 柯里化可以通过闭包先保存部分参数。
5. 闭包不是天然的内存泄漏，但长期引用可能让相关数据无法回收。
6. 不需要独立状态的方法可放到原型上，避免每个实例重复创建函数。
