`this` 是 JavaScript 中很容易混淆的概念。理解它的关键不是只看函数写在哪里，而是先判断函数以什么方式被调用。

## 1. this 不是函数本身，也不是词法作用域

先看一段函数调用链：

```js
function baz() {
  console.log("baz");
  bar();
}

function bar() {
  console.log("bar");
  foo();
}

function foo() {
  console.log("foo");
}

baz();
```

调用顺序是 `baz → bar → foo`，但这个调用链本身不能直接决定每个函数的 `this`。普通函数的 `this` 要根据调用位置和调用方式确定。

下面这种写法是不允许的：

```js
function fn() {
  // this = obj; // SyntaxError，不能直接给 this 赋值
}
```

如果想指定 `this`，应该通过对象方法调用、`call()`、`apply()`、`bind()` 或 `new` 等方式。

## 2. 默认绑定

普通函数直接调用时，会使用默认绑定。资料中的传统非严格脚本示例是：

```js
var name = "Jenny";

function person() {
  return this.name;
}

console.log(person()); // "Jenny"（浏览器非严格脚本）
```

这里 `person()` 前面没有调用对象。在传统浏览器非严格脚本中，`this` 会指向 `window`，而顶层 `var name` 又成为 `window.name`，所以得到 `"Jenny"`。

严格模式下，普通函数直接调用时 `this` 是 `undefined`：

```js
"use strict";

function showThis() {
  console.log(this);
}

showThis(); // undefined
```

因此不要在现代代码中依赖“默认指向 window”。

## 3. 隐式绑定：由调用对象决定

函数作为对象属性调用时，点号左边的对象通常会成为 `this`：

```js
function test() {
  console.log(this.x);
}

const obj = {};
obj.x = 1;
obj.m = test;

obj.m(); // 1
```

调用表达式是 `obj.m()`，因此 `this` 指向 `obj`。

### 3.1 只看调用点最近的一层

```js
const o = {
  a: 10,
  b: {
    fn() {
      console.log(this.a);
    },
  },
};

o.b.fn(); // undefined
```

调用点是 `o.b.fn()`，函数前面最近的调用对象是 `o.b`，所以 `this === o.b`。`o.b` 没有 `a`，结果为 `undefined`；JavaScript 不会因为外层对象 `o` 有 `a` 就自动向外查找。

如果给 `b` 自己增加 `a`：

```js
const o = {
  a: 10,
  b: {
    a: 12,
    fn() {
      console.log(this.a);
    },
  },
};

o.b.fn(); // 12
```

### 3.2 方法被取出后会丢失原对象

```js
var a = 10;

var o = {
  a: 12,
  b: {
    fn() {
      console.log(this.a);
      console.log(this);
    },
  },
};

var j = o.b.fn;
j();
```

`j()` 是普通函数直接调用，不再是 `o.b.fn()`，因此失去了 `o.b` 的隐式绑定。传统非严格浏览器脚本中 `this` 是 `window`，会输出全局的 `a`；严格模式下 `this` 是 `undefined`，访问 `this.a` 会报错。

## 4. new 绑定

函数通过 `new` 调用时，`this` 指向新创建的对象：

```js
function Test() {
  this.x = 1;
}

const obj = new Test();

console.log(obj.x); // 1
```

构造函数的返回值还会影响最终结果。

### 4.1 返回对象

```js
function Fn() {
  this.user = "xxx";
  return {};
}

const value = new Fn();

console.log(value.user); // undefined
```

显式返回对象时，这个对象会替代原本的新实例。

### 4.2 返回基本值或 null

```js
function ReturnNumber() {
  this.user = "xxx";
  return 1;
}

function ReturnNull() {
  this.user = "xxx";
  return null;
}

console.log(new ReturnNumber().user); // "xxx"
console.log(new ReturnNull().user); // "xxx"
```

构造函数返回普通基本值或 `null` 时，不会替换新创建的实例。

## 5. 显式绑定：apply、call、bind

`apply()`、`call()` 和 `bind()` 可以显式指定普通函数的 `this`。

```js
var x = 0;

function test() {
  console.log(this.x);
}

const obj = { x: 1 };

test.apply(obj); // 1
```

另一个例子：

```js
function foo() {
  console.log(this.a);
}

const obj1 = { a: 2, foo };
const obj2 = { a: 3, foo };

obj1.foo(); // 2
obj2.foo(); // 3
obj1.foo.call(obj2); // 3
obj2.foo.call(obj1); // 2
```

对象方法调用本来会隐式绑定，但 `call()` 又明确指定了另一个对象，所以显式绑定生效。

## 6. 箭头函数的 this

箭头函数没有自己的 `this`，它会从定义位置的外层作用域取得 `this`。

```js
const obj = {
  sayThis: () => {
    console.log(this);
  },
};

obj.sayThis();

const globalSay = obj.sayThis;
globalSay();
```

即使写成 `obj.sayThis()`，箭头函数也不会因为点号调用而把 `this` 绑定到 `obj`。在资料的浏览器非模块脚本环境中，两次都指向外层的 `window`。

### 6.1 DOM 事件

```js
const button = document.getElementById("mngb");

button.addEventListener("click", () => {
  console.log(this === window); // 传统脚本中为 true
});
```

箭头回调的 `this` 不是按钮。要取得触发监听器的元素，应使用事件对象：

```js
button.addEventListener("click", (event) => {
  event.currentTarget.textContent = "clicked button";
});
```

### 6.2 原型方法

```js
Cat.prototype.sayName = () => {
  return this.name;
};

const cat = new Cat("mm");
cat.sayName();
```

箭头函数不会把 `this` 指向 `cat`，所以需要动态实例 `this` 的原型方法应使用普通函数。

## 7. 绑定优先级

资料通过下面的例子比较了隐式绑定、显式绑定和 `new` 绑定：

```js
function foo(something) {
  this.a = something;
}

const obj1 = { foo };
const obj2 = {};

obj1.foo(2);
console.log(obj1.a); // 2

obj1.foo.call(obj2, 3);
console.log(obj2.a); // 3

const bar = new obj1.foo(4);
console.log(obj1.a); // 2
console.log(bar.a); // 4
```

这里可以看出：

```text
new 绑定 > call/apply 显式绑定 > 对象方法隐式绑定 > 默认绑定
```

`bind()` 返回的函数被 `new` 调用时，同样由 `new` 创建的新对象成为 `this`：

```js
function foo(something) {
  this.a = something;
}

const obj1 = {};
const bar = foo.bind(obj1);

bar(2);
console.log(obj1.a); // 2

const baz = new bar(3);
console.log(obj1.a); // 2
console.log(baz.a); // 3
```

## 8. 总结

1. 普通函数的 `this` 主要由调用方式决定。
2. 直接调用使用默认绑定；严格模式下 `this` 为 `undefined`。
3. `obj.fn()` 中的 `this` 指向调用点最近的 `obj`。
4. `call()`、`apply()`、`bind()` 可以显式指定 `this`。
5. `new` 调用时，`this` 指向新实例，并且优先级高于其他绑定。
6. 箭头函数没有自己的 `this`，而是继承定义位置外层的 `this`。
7. 判断题目时先看是否为箭头函数，再从调用表达式分析绑定方式。
