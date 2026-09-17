递归是函数在执行过程中直接或间接调用自身。尾递归是一种特殊递归：递归调用是函数返回前的最后一个操作，当前调用不再等待其他计算。

## 1. 从循环计算幂开始

计算 `x` 的 `n` 次方，可以使用循环：

```js
function pow(x, n) {
  let result = 1;

  for (let i = 0; i < n; i++) {
    result *= x;
  }

  return result;
}
```

同一个问题也可以递归定义：

```js
function pow(x, n) {
  if (n === 1) {
    return x;
  }

  return x * pow(x, n - 1);
}
```

数学关系是：

```text
pow(x, n) = x                         当 n = 1
pow(x, n) = x × pow(x, n - 1)         当 n > 1
```

## 2. 递归如何执行

计算 `pow(2, 4)`：

```text
pow(2, 4) = 2 × pow(2, 3)
pow(2, 3) = 2 × pow(2, 2)
pow(2, 2) = 2 × pow(2, 1)
pow(2, 1) = 2
```

到达基础条件 `n === 1` 后，结果再逐层返回：

```text
pow(2, 1) = 2
pow(2, 2) = 2 × 2 = 4
pow(2, 3) = 2 × 4 = 8
pow(2, 4) = 2 × 8 = 16
```

递归必须有：

1. 基础条件：什么时候停止；
2. 规模缩小：每次调用都更接近基础条件。

否则会无限调用，最终出现调用栈溢出。

## 3. 普通递归阶乘

```js
function factorial(n) {
  if (n === 1) return 1;
  return n * factorial(n - 1);
}

console.log(factorial(5)); // 120
```

每一层都要等待下一层返回后，再完成乘法：

```text
factorial(5)
5 × factorial(4)
5 × 4 × factorial(3)
5 × 4 × 3 × factorial(2)
5 × 4 × 3 × 2 × factorial(1)
```

调用栈深度随 `n` 增长，空间复杂度约为 `O(n)`。

## 4. 什么是尾递归

把中间结果放进参数，使递归调用成为最后一个操作：

```js
function factorial(n, total = 1) {
  if (n === 1) return total;
  return factorial(n - 1, n * total);
}

console.log(factorial(5, 1)); // 120
```

过程变成：

```text
factorial(5, 1)
factorial(4, 5)
factorial(3, 20)
factorial(2, 60)
factorial(1, 120)
返回 120
```

每层函数不再等待“递归结果回来后还要乘一次”，而是把累计结果直接传给下一次调用。

> 说明：尾递归只有在运行时实现尾调用优化时，才能真正复用栈帧。不能假设所有 JavaScript 引擎都会优化；处理非常深的数据时，循环通常更稳妥。

## 5. 尾递归计算数组总和

资料给出了通过累计参数求和的思路。改成不修改原数组的版本：

```js
function sumArray(array, index = 0, total = 0) {
  if (index === array.length) {
    return total;
  }

  return sumArray(array, index + 1, total + array[index]);
}

console.log(sumArray([1, 2, 3, 4])); // 10
```

`index` 控制当前读取位置，`total` 保存累计结果。

资料原示例使用 `arr.pop()` 并调用了不存在的 `sum()`，会修改输入且存在函数名错误。上面保留“累计参数”的核心思路，同时修正这两个问题。

## 6. 尾递归计算斐波那契数

斐波那契数列：

```text
1, 1, 2, 3, 5, 8, ...
```

使用两个累计参数：

```js
function fibonacci(n, current = 1, next = 1) {
  if (n <= 1) {
    return current;
  }

  return fibonacci(n - 1, next, current + next);
}

console.log(fibonacci(1)); // 1
console.log(fibonacci(2)); // 1
console.log(fibonacci(5)); // 5
```

每次调用把下一对相邻值传下去，不需要在递归返回后继续相加。

## 7. 递归展开嵌套数组

资料使用递归把多层数组变成一维数组：

```js
const source = [1, 2, 3, [1, 2, 3, [1, 2, 3]]];
```

```js
function flat(array = [], result = []) {
  array.forEach((value) => {
    if (Array.isArray(value)) {
      flat(value, result);
    } else {
      result.push(value);
    }
  });

  return result;
}

console.log(flat(source));
// [1, 2, 3, 1, 2, 3, 1, 2, 3]
```

递归适合这里，是因为数组的元素仍可能是同样结构的数组。遇到数组就继续处理，遇到普通值就放入结果。

现代 JavaScript 也有内置方法：

```js
source.flat(Infinity);
```

但手写递归有助于理解树形或嵌套结构遍历。

## 8. 递归转换对象键名

资料还展示了把嵌套对象中的大写属性名改成小写：

```js
const obj = {
  a: "1",
  b: {
    c: "2",
    D: {
      E: "3",
    },
  },
};
```

```js
function keysLower(value) {
  if (Array.isArray(value)) {
    return value.map(keysLower);
  }

  if (value === null || typeof value !== "object") {
    return value;
  }

  const result = {};

  for (const [key, childValue] of Object.entries(value)) {
    result[key.toLowerCase()] = keysLower(childValue);
  }

  return result;
}
```

```js
console.log(keysLower(obj));
// {
//   a: "1",
//   b: {
//     c: "2",
//     d: {
//       e: "3"
//     }
//   }
// }
```

资料原代码直接删除和重建属性；上面返回新对象，避免一边遍历一边修改原对象，也正确处理 `null` 和数组。

## 9. 递归的应用场景

递归特别适合“一个问题包含相同结构的子问题”：

- 遍历 DOM 树；
- 遍历文件目录；
- 处理树和图；
- 展开嵌套数组；
- 深拷贝对象；
- 转换嵌套数据；
- 分治算法；
- 阶乘、幂和数列等数学定义。

如果输入层级可能非常深，应该考虑循环、显式栈或队列，避免依赖有限的函数调用栈。

## 10. 总结

1. 递归函数直接或间接调用自身，必须有基础条件并不断缩小问题。
2. 普通递归在返回后仍有计算，因此需要保留每一层调用状态。
3. 尾递归把累计结果作为参数传递，让递归调用成为最后一步。
4. JavaScript 引擎不一定进行尾调用优化，极深递归仍可能栈溢出。
5. 嵌套数组、对象和树形结构是递归的典型应用。
6. 深度不可控时，使用循环或显式数据结构通常更安全。
