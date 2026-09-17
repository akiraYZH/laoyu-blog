`call()`、`apply()` 和 `bind()` 都可以改变普通函数执行时的 `this`。三者的主要区别是：`call()` 和 `apply()` 立即执行函数，`bind()` 返回一个绑定后的新函数；`call()` 接收逐个参数，`apply()` 接收参数数组。

## 1. 为什么需要改变 this

```js
var name = "lucy";

var obj = {
  name: "martin",
  say() {
    console.log(this.name);
  },
};

obj.say(); // "martin"
setTimeout(obj.say, 0); // "lucy"（传统非严格浏览器脚本）
```

执行 `obj.say()` 时，`this` 指向 `obj`。但传给 `setTimeout` 的只是函数 `obj.say`，定时器之后以普通函数方式调用它，原来的对象调用关系已经丢失。

可以用 `bind()` 保留 `this`：

```js
setTimeout(obj.say.bind(obj), 0); // "martin"
```

## 2. apply

`apply()` 立即调用函数，第一个参数指定 `this`，第二个参数是数组或类数组参数列表：

```js
function fn(...args) {
  console.log(this, args);
}

const obj = {
  myname: "张三",
};

fn.apply(obj, [1, 2]);
// this 是 obj，args 是 [1, 2]
```

普通调用没有显式指定 `this`：

```js
fn(1, 2);
```

在传统浏览器非严格脚本中，`this` 会指向 `window`；严格模式下则为 `undefined`。

资料还展示了传入 `null` 和 `undefined`：

```js
fn.apply(null, [1, 2]);
fn.apply(undefined, [1, 2]);
```

非严格模式下它们会被替换为全局对象；严格模式下会分别保留为 `null` 和 `undefined`。

`apply()` 适合参数已经保存在数组中的情况：

```js
const numbers = [3, 8, 2, 5];
const max = Math.max.apply(null, numbers);

console.log(max); // 8
```

现代代码也可以使用展开语法：

```js
const max = Math.max(...numbers);
```

## 3. call

`call()` 同样立即执行函数，但参数从第二位开始逐个传入：

```js
function fn(...args) {
  console.log(this, args);
}

const obj = {
  myname: "张三",
};

fn.call(obj, 1, 2);
// this 是 obj，args 是 [1, 2]
```

对比参数形式：

```js
fn.apply(obj, [1, 2]);
fn.call(obj, 1, 2);
```

`call()` 传入 `null` 或 `undefined` 时，严格模式与非严格模式的处理和 `apply()` 相同：

```js
fn.call(null, 1, 2);
fn.call(undefined, 1, 2);
```

> 说明：资料中的 `fn.call(null, [1, 2])` 会把整个数组作为一个参数传入，并不等同于 `fn.apply(null, [1, 2])`。如果需要两个独立参数，应写成 `fn.call(null, 1, 2)`。

## 4. bind

`bind()` 不立即执行原函数，而是返回一个新函数，并保存指定的 `this`：

```js
function fn(...args) {
  console.log(this, args);
}

const obj = {
  myname: "张三",
};

const bindFn = fn.bind(obj);

bindFn(1, 2); // this 是 obj
fn(1, 2); // 原函数本身没有被永久修改
```

`bind()` 还可以预先保存一部分参数：

```js
function add(a, b) {
  return a + b;
}

const addOne = add.bind(null, 1);

console.log(addOne(2)); // 3
```

下面两种写法都能得到参数 `1` 和 `2`：

```js
fn.bind(obj, 1, 2)();
fn.bind(obj, 1)(2);
```

第一种在绑定时预设全部参数，第二种在绑定时预设一部分，调用时再补充其余参数。

## 5. 三者对比

| 方法 | 是否立即执行 | 参数形式 | 返回值 |
| --- | --- | --- | --- |
| `fn.apply(obj, [1, 2])` | 是 | 数组或类数组 | 原函数执行结果 |
| `fn.call(obj, 1, 2)` | 是 | 参数逐个传入 | 原函数执行结果 |
| `fn.bind(obj, 1)` | 否 | 可预设部分参数 | 新的绑定函数 |

共同点：

- 都是函数的方法；
- 都可以指定普通函数执行时的 `this`；
- 箭头函数没有自己的 `this`，因此不能用这三个方法把箭头函数的 `this` 改成另一个对象。

不同点：

- `apply()` 和 `call()` 立即执行；
- `bind()` 只创建新函数，等待以后调用；
- `apply()` 的参数集中在数组中，`call()` 和 `bind()` 的参数逐个写出。

## 6. bind 与 new

绑定函数仍然可以通过 `new` 调用。此时 `new` 创建的新实例会成为 `this`，而不是 `bind()` 指定的对象：

```js
function Person(name) {
  this.name = name;
}

const target = {};
const BoundPerson = Person.bind(target);
const person = new BoundPerson("Tom");

console.log(person.name); // "Tom"
console.log(target.name); // undefined
console.log(person instanceof Person); // true
```

这也是手写 `bind` 时不能忽略的情况。

## 7. 手写 bind

下面的实现保留了资料中的三个核心目标：确认调用者是函数、合并预设参数和调用参数、兼容 `new` 调用。

```js
Function.prototype.myBind = function (context, ...boundArgs) {
  if (typeof this !== "function") {
    throw new TypeError("myBind 必须由函数调用");
  }

  const originalFn = this;

  function BoundFn(...callArgs) {
    const thisArg = this instanceof BoundFn ? this : context;
    return originalFn.apply(thisArg, [...boundArgs, ...callArgs]);
  }

  if (originalFn.prototype) {
    BoundFn.prototype = Object.create(originalFn.prototype);
    BoundFn.prototype.constructor = BoundFn;
  }

  return BoundFn;
};
```

普通绑定：

```js
function introduce(greeting, punctuation) {
  return `${greeting}, ${this.name}${punctuation}`;
}

const user = { name: "Tom" };
const sayHello = introduce.myBind(user, "Hello");

console.log(sayHello("!")); // "Hello, Tom!"
```

作为构造函数调用：

```js
function Person(name) {
  this.name = name;
}

const target = {};
const BoundPerson = Person.myBind(target);
const person = new BoundPerson("Tom");

console.log(person.name); // "Tom"
console.log(person instanceof Person); // true
```

> 说明：这是用于解释机制的简化实现，原生 `bind()` 还涉及 `length`、`name`、构造行为等规范细节，不能用简单代码完全替代。

## 8. 总结

1. `apply()`、`call()` 和 `bind()` 都可以指定普通函数的 `this`。
2. `apply()` 和 `call()` 立即执行，区别在于参数列表形式。
3. `bind()` 返回新函数，可以预设 `this` 和部分参数。
4. 非严格模式下，`null`、`undefined` 可能被替换为全局对象；严格模式下不会。
5. 绑定函数通过 `new` 调用时，新实例优先成为 `this`。
6. 箭头函数的 `this` 来自外层词法环境，不能被这三个方法重新绑定。
