作用域决定变量和函数在代码中的可见范围。换句话说，它回答的是：**当前这行代码能够访问哪些变量？**

作用域链则描述变量的查找过程：当前作用域找不到变量时，JavaScript 会到外层作用域继续查找，直到全局作用域。

## 一、什么是作用域

先看一个简单例子：

```js
function myFunction() {
  let inVariable = "函数内部变量";
}

myFunction();
console.log(inVariable);
// ReferenceError: inVariable is not defined
```

`inVariable` 在 `myFunction` 内部声明，因此只能在这个函数内部访问。函数外部看不到它。

这说明作用域限制了变量的可见范围。JavaScript 中常见的作用域可以分为：

- 全局作用域；
- 函数作用域；
- 块级作用域。

## 二、全局作用域

在传统 JavaScript 脚本中，声明在所有函数和代码块之外的变量处于全局作用域。

```js
var greeting = "Hello World!";

function greet() {
  console.log(greeting);
}

greet(); // "Hello World!"
```

执行 `greet()` 时，函数内部没有声明 `greeting`，但仍然可以访问外层的全局变量。

查找过程是：

```text
greet 函数作用域
└── 没有 greeting
    └── 全局作用域
        └── 找到 greeting
```

因此输出 `"Hello World!"`。

全局变量可以被程序中的很多位置访问，但也容易造成命名冲突。理解全局作用域的重点是它处于作用域链的最外层，而不是鼓励大量使用全局变量。

## 三、函数作用域

在函数内部声明的变量处于函数作用域，只能在当前函数以及它内部定义的函数中访问。

```js
function greet() {
  var greeting = "Hello World!";
  console.log(greeting);
}

greet(); // "Hello World!"

console.log(greeting);
// ReferenceError: greeting is not defined
```

在 `greet()` 内部访问 `greeting` 没有问题，因为变量就在当前函数作用域中。

函数执行结束后，在外部执行 `console.log(greeting)`，JavaScript 会先查找全局作用域。全局作用域没有这个变量，因此抛出 `ReferenceError`。

可以画成：

```text
全局作用域
│
└── greet 函数作用域
    └── greeting = "Hello World!"
```

外层作用域不能反过来访问内层函数的局部变量。

## 四、块级作用域

ES6 引入的 `let` 和 `const` 具有块级作用域。代码块通常由一对大括号 `{}` 表示。

```js
{
  let greeting = "Hello World!";
  var lang = "English";

  console.log(greeting); // "Hello World!"
}

console.log(lang); // "English"
console.log(greeting);
// ReferenceError: greeting is not defined
```

这段代码中：

- `greeting` 使用 `let` 声明，只在大括号内部有效；
- `lang` 使用 `var` 声明，`var` 不受普通代码块限制，因此块外仍能访问。

关系可以表示为：

```text
外层作用域
├── lang = "English"
│
└── 块级作用域 { }
    └── greeting = "Hello World!"
```

如果把 `lang` 也改成 `let` 或 `const`，它同样只能在代码块内部使用。

```js
{
  const lang = "English";
}

console.log(lang);
// ReferenceError: lang is not defined
```

因此可以这样记：

```text
var：主要受函数作用域限制
let、const：受块级作用域限制
```

## 五、什么是词法作用域

JavaScript 使用词法作用域，也叫静态作用域。

词法作用域的意思是：**函数可以访问哪些变量，在函数写在哪里时就已经确定，而不是由函数在哪里被调用决定。**

看 PDF 中的例子：

```js
var a = 2;

function foo() {
  console.log(a);
}

function bar() {
  var a = 3;
  foo();
}

bar(); // 2
```

很多人第一次看到会认为结果是 `3`，因为 `foo()` 是在 `bar()` 内部调用的，而 `bar()` 中存在 `a = 3`。

实际结果是 `2`。原因是 `foo` 定义在全局作用域中，它的外层作用域是全局作用域，而不是调用它的 `bar` 函数作用域。

按照函数的定义位置画图：

```text
全局作用域
├── a = 2
├── foo 函数作用域
│   └── 外层作用域：全局作用域
│
└── bar 函数作用域
    ├── a = 3
    └── 外层作用域：全局作用域
```

执行 `foo()` 时，查找 `a` 的过程是：

```text
foo 函数内部
└── 没有 a
    └── foo 定义位置的外层：全局作用域
        └── 找到 a = 2
```

`foo` 不会因为在 `bar` 中被调用，就进入 `bar` 的作用域查找变量。

所以判断词法作用域时应该看：

```text
函数写在哪里？
```

而不是：

```text
函数从哪里调用？
```

## 六、什么是作用域链

当代码使用一个变量时，JavaScript 会先在当前作用域查找。如果没有找到，就到外层作用域继续查找。一层层向外查找形成的路径，就是作用域链。

可以把多个嵌套作用域理解成一栋楼：

```text
顶层：全局作用域
  ↑
外层函数作用域
  ↑
内层函数作用域
  ↑
当前作用域
```

变量查找从当前楼层开始。当前层没有，就向上一层；找到后停止。如果到达全局作用域仍然找不到，就会报错。

## 七、用嵌套函数理解作用域链

```js
var sex = "男";

function person() {
  var name = "张三";

  function student() {
    var age = 18;
    console.log(name);
    console.log(sex);
  }

  student();
  console.log(age);
}

person();
```

先画出它们的定义关系：

```text
全局作用域
├── sex = "男"
│
└── person 函数作用域
    ├── name = "张三"
    │
    └── student 函数作用域
        └── age = 18
```

### student 如何找到 name

`student` 内部没有 `name`，于是向外层 `person` 查找：

```text
student
└── 没有 name
    └── person
        └── 找到 name = "张三"
```

所以第一行输出：

```text
张三
```

### student 如何找到 sex

`student` 内部没有 `sex`，`person` 中也没有，于是继续到全局作用域：

```text
student
└── 没有 sex
    └── person
        └── 没有 sex
            └── 全局作用域
                └── 找到 sex = "男"
```

所以第二行输出：

```text
男
```

### person 为什么找不到 age

`age` 声明在更内层的 `student` 中。变量查找只能从当前作用域向外层查找，不能从外层进入内层函数查找。

```text
person
└── 自身没有 age
    └── 全局作用域也没有 age
        └── ReferenceError
```

因此最后一行会报错：

```text
ReferenceError: age is not defined
```

## 八、作用域链的查找方向

作用域链只能由内向外查找：

```text
内层函数 → 外层函数 → 全局作用域
```

不能反向查找：

```text
全局作用域 ✘ 进入函数内部
外层函数   ✘ 进入内层函数
```

因此：

- 内层函数可以访问外层函数的变量；
- 外层函数不能直接访问内层函数的局部变量；
- 找到第一个同名变量后，查找立即停止；
- 一直找不到变量时，会抛出 `ReferenceError`。

## 总结

1. 作用域决定变量和函数在哪些位置可见。
2. JavaScript 常见作用域包括全局作用域、函数作用域和块级作用域。
3. `var` 主要具有函数作用域，`let` 和 `const` 具有块级作用域。
4. JavaScript 使用词法作用域，函数的外层作用域由定义位置决定。
5. 查找变量时先查当前作用域，再沿外层作用域逐级查找。
6. 这条由内向外的查找路径就是作用域链。
7. 内层可以访问外层变量，外层不能直接访问内层变量。

判断一道作用域题时，先画出函数的定义嵌套关系，再从使用变量的位置向外逐层查找，就不容易出错。

## 参考资料

- [MDN：作用域](https://developer.mozilla.org/zh-CN/docs/Glossary/Scope)
- [MDN：let](https://developer.mozilla.org/zh-CN/docs/Web/JavaScript/Reference/Statements/let)
