函数式编程是一种以函数作为主要抽象单位的编程方式。JavaScript 中，函数可以赋值给变量、作为参数传入、作为返回值返回，因此很适合使用映射、高阶函数、柯里化和函数组合等思想。

## 1. 从循环到 map

命令式写法描述每一步如何修改数组：

```js
const array = [0, 1, 2, 3];

for (let i = 0; i < array.length; i++) {
  array[i] = Math.pow(array[i], 2);
}
```

函数式写法更关注“把每个数字映射为平方”：

```js
const result = [0, 1, 2, 3].map((number) =>
  Math.pow(number, 2),
);
```

`map()` 返回新数组，不需要手动管理索引和修改过程。

## 2. 纯函数

纯函数具有两个重要特点：

1. 相同输入始终得到相同输出；
2. 执行过程不修改外部可观察状态。

```js
const double = (value) => value * 2;
```

`double(2)` 每次都得到 `4`，也不会修改外部变量，因此容易测试：

```js
test("double(2) 应该得到 4", () => {
  expect(double(2)).toBe(4);
});
```

依赖当前时间、随机数或修改全局变量的函数就不是纯函数：

```js
let total = 0;

function addToTotal(value) {
  total += value;
}
```

函数式编程倾向于把计算写成纯函数，把网络、DOM、日志等副作用集中到边界位置。

## 3. 高阶函数

高阶函数接收函数作为参数，或者返回一个函数。

资料手写了 `forEach`：

```js
const forEach = function (array, fn) {
  for (let i = 0; i < array.length; i++) {
    fn(array[i]);
  }
};

const array = [1, 2, 3];

forEach(array, (item) => {
  console.log(item);
});
```

`forEach` 不关心具体要对元素做什么，而是把这一部分交给传入的 `fn`。

## 4. once：只执行一次

返回函数的高阶函数可以通过闭包保存状态：

```js
const once = (fn) => {
  let done = false;

  return function (...args) {
    if (done) {
      console.log("函数已经执行过");
      return;
    }

    done = true;
    return fn.apply(this, args);
  };
};
```

```js
const initialize = once(() => {
  console.log("初始化");
});

initialize(); // "初始化"
initialize(); // "函数已经执行过"
```

> 说明：资料原示例写成 `fn.apply(this, fn)`，第二个参数应是参数数组或类数组，而不是函数本身。上面改为收集并传入实际调用参数。

## 5. 柯里化

柯里化把一个接收多个参数的函数，转换为连续接收较少参数的函数。

原函数：

```js
const fn = (x, y) => x + y;
```

两参数版本：

```js
const curry = function (fn) {
  return function (x) {
    return function (y) {
      return fn(x, y);
    };
  };
};

const myFn = curry(fn);

console.log(myFn(1)(2)); // 3
```

闭包让内层函数记住先传入的 `x`。

## 6. 通用 curry

可以根据原函数的 `length` 判断是否收集到足够参数：

```js
const curry = function (fn) {
  return function curriedFn(...args) {
    if (args.length < fn.length) {
      return function (...nextArgs) {
        return curriedFn(...args, ...nextArgs);
      };
    }

    return fn(...args);
  };
};
```

```js
const sum = (x, y, z, a) => x + y + z + a;
const curriedSum = curry(sum);

console.log(curriedSum(1)(2)(3)(1)); // 7
console.log(curriedSum(1, 2)(3, 1)); // 7
```

柯里化适合复用部分参数，但不应为了形式把简单调用变得难读。

## 7. 函数组合 compose

组合把多个小函数连接成一个函数。资料先给出两个函数：

```js
function aFn(a) {
  return a * 2;
}

function bFn(b) {
  return b * 3;
}
```

组合函数从右向左执行：

```js
const compose = (a, b) => (value) => a(b(value));

const myFn = compose(aFn, bFn);

console.log(myFn(2)); // bFn(2) = 6，再 aFn(6) = 12
```

通用写法：

```js
const compose = (...functions) => (value) =>
  [...functions]
    .reverse()
    .reduce((result, fn) => fn(result), value);
```

复制数组后再 `reverse()`，避免改变传入的函数数组。

## 8. pipe

`pipe` 与 `compose` 的区别只是执行方向：`pipe` 从左向右。

```js
const pipe = (...functions) => (value) =>
  functions.reduce((result, fn) => fn(result), value);
```

```js
const double = (value) => value * 2;
const addOne = (value) => value + 1;

const calculate = pipe(double, addOne);

console.log(calculate(3)); // 7
```

过程是先 `double(3)` 得到 `6`，再 `addOne(6)` 得到 `7`。

## 9. 不可变数据

函数式写法倾向于返回新值，而不是直接修改输入：

```js
const user = { name: "Tom", age: 20 };

const olderUser = {
  ...user,
  age: user.age + 1,
};

console.log(user.age); // 20
console.log(olderUser.age); // 21
```

这并不意味着所有对象都自动深度不可变；展开语法只进行浅拷贝。重点是让数据变化方向清楚、便于追踪。

## 10. 总结

1. 函数式编程把计算拆成可组合的小函数。
2. 纯函数在相同输入下产生相同输出，并避免修改外部状态。
3. 高阶函数可以接收或返回函数，`map`、`forEach`、`once` 都体现这一点。
4. 柯里化通过闭包逐步收集参数。
5. `compose` 从右向左执行函数，`pipe` 从左向右执行。
6. 不可变更新让数据变化更容易追踪，但要注意浅拷贝边界。
