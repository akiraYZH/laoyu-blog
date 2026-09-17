JavaScript 数组方法可以按用途分为：添加、删除、截取、查找、排序、转换和迭代。学习时还要区分方法是否修改原数组。

## 1. 添加元素

### 1.1 push

`push()` 在数组末尾添加一个或多个元素，并返回新长度：

```js
const colors = [];
const count = colors.push("red", "green");

console.log(count); // 2
console.log(colors); // ["red", "green"]
```

### 1.2 unshift

`unshift()` 在数组开头添加元素，也返回新长度：

```js
const colors = [];
const count = colors.unshift("red", "green");

console.log(count); // 2
console.log(colors); // ["red", "green"]
```

### 1.3 splice 插入

`splice(start, deleteCount, ...items)` 可以在指定位置插入元素。删除数量写 `0` 时只插入：

```js
const colors = ["red", "green", "blue"];
const removed = colors.splice(1, 0, "yellow", "orange");

console.log(colors);
// ["red", "yellow", "orange", "green", "blue"]
console.log(removed); // []
```

### 1.4 concat

`concat()` 返回拼接后的新数组，不修改原数组：

```js
const colors = ["red", "green", "blue"];
const colors2 = colors.concat("yellow", ["black", "brown"]);

console.log(colors); // ["red", "green", "blue"]
console.log(colors2);
// ["red", "green", "blue", "yellow", "black", "brown"]
```

## 2. 删除元素

### 2.1 pop

`pop()` 删除并返回最后一个元素：

```js
const colors = ["red", "green"];
const item = colors.pop();

console.log(item); // "green"
console.log(colors.length); // 1
```

### 2.2 shift

`shift()` 删除并返回第一个元素：

```js
const colors = ["red", "green"];
const item = colors.shift();

console.log(item); // "red"
console.log(colors.length); // 1
```

### 2.3 splice 删除

```js
const colors = ["red", "green", "blue"];
const removed = colors.splice(0, 1);

console.log(colors); // ["green", "blue"]
console.log(removed); // ["red"]
```

`splice()` 会修改原数组，并把被删除的元素放在新数组中返回。

## 3. 截取数组

`slice(start, end)` 返回从 `start` 到 `end` 之前的浅拷贝，不修改原数组：

```js
const colors = ["red", "green", "blue", "yellow", "purple"];
const colors2 = colors.slice(1);
const colors3 = colors.slice(1, 4);

console.log(colors);
// ["red", "green", "blue", "yellow", "purple"]
console.log(colors2);
// ["green", "blue", "yellow", "purple"]
console.log(colors3);
// ["green", "blue", "yellow"]
```

## 4. 替换数组元素

`splice()` 先删除指定数量，再插入新元素：

```js
const colors = ["red", "green", "blue"];
const removed = colors.splice(1, 1, "red", "purple");

console.log(colors); // ["red", "red", "purple", "blue"]
console.log(removed); // ["green"]
```

可以这样记住 `splice`：

```text
splice(从哪里开始, 删除几个, 插入哪些元素)
```

## 5. 查找元素

### 5.1 indexOf

`indexOf()` 返回第一次出现的索引，找不到返回 `-1`：

```js
const numbers = [1, 2, 3, 4, 5, 4, 3, 2, 1];

console.log(numbers.indexOf(4)); // 3
console.log(numbers.indexOf(9)); // -1
```

### 5.2 includes

`includes()` 直接返回是否包含指定元素：

```js
const numbers = [1, 2, 3, 4, 5];

console.log(numbers.includes(4)); // true
console.log(numbers.includes(9)); // false
```

### 5.3 find

`find()` 返回第一个让回调函数结果为 `true` 的元素：

```js
const people = [
  { name: "Matt", age: 27 },
  { name: "Nicholas", age: 29 },
];

const person = people.find((element, index, array) => element.age < 28);

console.log(person); // { name: "Matt", age: 27 }
```

找不到时返回 `undefined`。如果需要索引，可以使用 `findIndex()`。

## 6. 排序和反转

### 6.1 reverse

`reverse()` 反转数组，并修改原数组：

```js
const values = [1, 2, 3, 4, 5];

values.reverse();

console.log(values); // [5, 4, 3, 2, 1]
```

### 6.2 sort

`sort()` 如果不提供比较函数，会按字符串形式排序。数字排序应明确提供比较函数：

```js
function compare(value1, value2) {
  if (value1 < value2) return -1;
  if (value1 > value2) return 1;
  return 0;
}

const values = [0, 1, 5, 10, 15];
values.sort(compare);

console.log(values); // [0, 1, 5, 10, 15]
```

更简洁的数字升序写法：

```js
values.sort((a, b) => a - b);
```

`sort()` 会修改原数组。需要保留原顺序时，可以先复制：

```js
const sorted = [...values].sort((a, b) => a - b);
```

## 7. 转换为字符串

`join(separator)` 用指定分隔符连接所有元素：

```js
const colors = ["red", "green", "blue"];

console.log(colors.join(",")); // "red,green,blue"
console.log(colors.join("||")); // "red||green||blue"
```

## 8. 迭代方法

常见迭代回调会接收三个参数：

```text
当前元素 item、当前索引 index、原数组 array
```

### 8.1 some

只要有一个元素满足条件，`some()` 就返回 `true`：

```js
const numbers = [1, 2, 3, 4, 5, 4, 3, 2, 1];
const result = numbers.some((item, index, array) => item > 2);

console.log(result); // true
```

### 8.2 every

只有所有元素都满足条件，`every()` 才返回 `true`：

```js
const numbers = [1, 2, 3, 4, 5, 4, 3, 2, 1];
const result = numbers.every((item, index, array) => item > 2);

console.log(result); // false
```

### 8.3 forEach

`forEach()` 对每个元素执行回调，主要用于遍历产生副作用：

```js
const numbers = [1, 2, 3];

numbers.forEach((item, index, array) => {
  console.log(index, item);
});
```

它不会因为回调中的普通 `return` 提前结束整个遍历。

### 8.4 filter

`filter()` 返回所有满足条件的元素组成的新数组：

```js
const numbers = [1, 2, 3, 4, 5, 4, 3, 2, 1];
const result = numbers.filter((item, index, array) => item > 2);

console.log(result); // [3, 4, 5, 4, 3]
```

### 8.5 map

`map()` 把每个元素转换成新值，并返回新数组：

```js
const numbers = [1, 2, 3, 4, 5, 4, 3, 2, 1];
const result = numbers.map((item, index, array) => item * 2);

console.log(result); // [2, 4, 6, 8, 10, 8, 6, 4, 2]
```

## 9. 是否修改原数组

| 修改原数组 | 不修改原数组 |
| --- | --- |
| `push`、`unshift`、`pop`、`shift` | `concat`、`slice` |
| `splice`、`reverse`、`sort` | `indexOf`、`includes`、`find` |
|  | `some`、`every`、`filter`、`map` |

`forEach()` 本身不替换数组，但回调可以主动修改元素或外部状态，因此要看具体代码。

## 10. 总结

1. 数组末尾操作使用 `push()`、`pop()`，开头操作使用 `unshift()`、`shift()`。
2. `splice()` 可以插入、删除和替换，并会修改原数组。
3. `slice()` 和 `concat()` 返回新数组。
4. 查找可使用 `indexOf()`、`includes()`、`find()`。
5. `reverse()` 和 `sort()` 会改变原数组。
6. 条件遍历使用 `some()`、`every()`、`filter()`，转换元素使用 `map()`，普通遍历使用 `forEach()`。
