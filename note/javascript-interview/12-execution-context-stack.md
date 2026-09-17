执行上下文描述 JavaScript 代码运行时所需的环境，执行栈记录这些上下文进入和离开的顺序。二者一起解释了变量如何被找到、函数调用为什么有先后顺序，以及递归为什么可能造成栈溢出。

## 1. 什么是执行上下文

简单来说，只要有 JavaScript 代码运行，就会运行在某个执行上下文中。

执行上下文常分为三类：

1. 全局执行上下文：一个脚本运行时会先创建全局上下文；
2. 函数执行上下文：每次调用函数都会创建一个新的函数上下文；
3. `eval` 执行上下文：运行 `eval()` 内代码时创建，实际开发很少使用，也不建议依赖。

```js
var sayHello = "Hello";

function person() {
  var first = "David";
  var last = "Shariff";

  function firstName() {
    return first;
  }

  function lastName() {
    return last;
  }

  alert(`${sayHello} ${firstName()} ${lastName()}`);
}

person();
```

这段代码会涉及：

- 一个全局执行上下文；
- 调用 `person()` 时创建的函数执行上下文；
- 调用 `firstName()` 时创建的函数执行上下文；
- 调用 `lastName()` 时创建的函数执行上下文。

每次调用函数都会创建新的上下文，因此同一个函数调用两次，也会得到两个彼此独立的函数执行上下文。

## 2. 执行上下文的生命周期

资料把执行上下文的生命周期分为三个阶段：

```text
创建阶段 → 执行阶段 → 回收阶段
```

## 3. 创建阶段

函数刚被调用、还没有真正执行函数体语句时，会先建立运行所需的信息。资料将其概括为三部分：

1. 确定 `this`，也叫 `ThisBinding`；
2. 创建词法环境 `LexicalEnvironment`；
3. 创建变量环境 `VariableEnvironment`。

可以用伪代码表示：

```js
ExecutionContext = {
  ThisBinding: "<this value>",
  LexicalEnvironment: { /* ... */ },
  VariableEnvironment: { /* ... */ },
};
```

### 3.1 ThisBinding

执行上下文会根据函数的调用方式确定 `this`。普通函数的 `this` 不是在函数定义时固定，而是在调用时确定。

```js
function showName() {
  console.log(this.name);
}

const user = { name: "Tom", showName };

user.showName(); // this 是 user
showName.call({ name: "Lucy" }); // this 是传入的对象
```

### 3.2 词法环境

词法环境可以理解为“标识符与值之间的映射，以及对外层环境的引用”。它包含环境记录和 `outer` 引用。

全局环境没有更外层环境，因此 `outer` 是 `null`：

```js
GlobalExecutionContext = {
  LexicalEnvironment: {
    EnvironmentRecord: {
      Type: "Object",
      // 全局标识符绑定
    },
    outer: null,
  },
};
```

函数环境保存函数内部声明、参数和 `arguments`，同时通过 `outer` 指向全局环境或外层函数环境：

```js
FunctionExecutionContext = {
  LexicalEnvironment: {
    EnvironmentRecord: {
      Type: "Declarative",
      // 参数、函数内部标识符
    },
    outer: "<Global or outer function environment reference>",
  },
};
```

这个 `outer` 引用正是沿外层环境查找变量的基础。

### 3.3 变量环境

变量环境本身也是词法环境。按照资料使用的 ES6 教学模型：

- 词法环境记录函数声明以及 `let`、`const` 绑定；
- 变量环境记录 `var` 绑定。

看下面的例子：

```js
let a = 20;
const b = 30;
var c;

function multiply(e, f) {
  var g = 20;
  return e * f * g;
}

c = multiply(20, 30);
```

创建全局上下文时，可以用下面的伪结构理解：

```js
GlobalExecutionContext = {
  ThisBinding: "<Global Object>",

  LexicalEnvironment: {
    EnvironmentRecord: {
      a: "<uninitialized>",
      b: "<uninitialized>",
      multiply: "<function>",
    },
    outer: null,
  },

  VariableEnvironment: {
    EnvironmentRecord: {
      c: undefined,
    },
    outer: null,
  },
};
```

调用 `multiply(20, 30)` 时，函数上下文可以理解为：

```js
FunctionExecutionContext = {
  ThisBinding: "<this value>",

  LexicalEnvironment: {
    EnvironmentRecord: {
      Arguments: { 0: 20, 1: 30, length: 2 },
      e: 20,
      f: 30,
    },
    outer: "<GlobalLexicalEnvironment>",
  },

  VariableEnvironment: {
    EnvironmentRecord: {
      g: undefined,
    },
    outer: "<GlobalLexicalEnvironment>",
  },
};
```

这些代码是用于解释内部关系的伪代码，不是浏览器中可以直接读取的真实对象。

## 4. 为什么 var 是 undefined，而 let/const 不能提前访问

创建阶段会扫描声明：

- `var` 绑定会先被初始化为 `undefined`；
- `let` 和 `const` 绑定已经建立，但在执行到声明之前保持未初始化状态。

```js
console.log(c); // undefined
var c = 10;
```

```js
console.log(a); // ReferenceError
let a = 20;
```

`let` 和 `const` 从作用域开始到完成初始化之间的区域，通常称为暂时性死区。它们不是“不提升”，而是提升后仍处于未初始化状态，不能提前访问。

## 5. 执行阶段

进入执行阶段后，JavaScript 按顺序执行代码，并给变量赋值：

```js
let a = 20;
const b = 30;
var c;
```

此时 `a` 和 `b` 完成初始化，`c` 仍是 `undefined`，直到后面执行：

```js
c = multiply(20, 30);
```

如果代码访问一个在当前及外层环境中都找不到的标识符，会抛出 `ReferenceError`；如果访问的是一个已经存在但当前值为 `undefined` 的变量，则得到 `undefined`。

## 6. 回收阶段

函数执行完成后，对应的执行上下文会从执行栈弹出。没有继续被引用的数据，之后可以由垃圾回收机制清理。

闭包可能让某些外层环境在函数返回后继续被引用，因此“函数结束”不代表所有相关数据一定立即释放。

## 7. 什么是执行栈

执行栈也叫调用栈，采用后进先出（LIFO）结构，用来保存代码运行期间创建的执行上下文。

```text
栈顶：当前正在执行的上下文
...
栈底：全局执行上下文
```

JavaScript 开始执行脚本时，先创建全局执行上下文并压入栈。调用函数时，创建新的函数执行上下文并压到栈顶；函数执行完毕，对应上下文从栈顶弹出。

## 8. 执行栈示例

```js
let a = "Hello World!";

function first() {
  console.log("Inside first function");
  second();
  console.log("Again inside first function");
}

function second() {
  console.log("Inside second function");
}

first();
console.log("Inside Global Execution Context");
```

执行过程如下：

1. 创建全局执行上下文并压入执行栈；
2. 调用 `first()`，创建 `first` 函数上下文并入栈；
3. `first()` 内调用 `second()`，创建 `second` 函数上下文并入栈；
4. `second()` 执行结束，其上下文弹出；
5. 回到 `first()` 继续执行，结束后 `first` 上下文弹出；
6. 回到全局上下文，执行最后一条日志；
7. 脚本完成，全局执行上下文退出。

栈的变化可以表示为：

```text
[Global]
[Global, first]
[Global, first, second]
[Global, first]
[Global]
[]
```

输出顺序是：

```text
Inside first function
Inside second function
Again inside first function
Inside Global Execution Context
```

## 9. 总结

1. 执行上下文保存代码运行所需的 `this`、词法环境和变量环境等信息。
2. 全局脚本有全局执行上下文，每次函数调用都会创建新的函数执行上下文。
3. 生命周期包括创建、执行和回收阶段。
4. 创建阶段会建立声明；`var` 初始化为 `undefined`，`let`、`const` 在声明执行前保持未初始化。
5. 执行栈采用后进先出结构，栈顶永远是当前正在运行的上下文。
6. 函数调用时上下文入栈，函数完成时上下文出栈，控制权回到下一个栈顶上下文。
