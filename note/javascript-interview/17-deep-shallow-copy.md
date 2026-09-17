浅拷贝和深拷贝的区别，关键在于嵌套对象是否仍然共享引用。

- 浅拷贝：只复制对象最外层属性；嵌套对象仍与原对象共享引用。
- 深拷贝：递归复制嵌套对象，使新对象与原对象不再共享这些内部引用。

## 1. 手写浅拷贝

```js
function shallowClone(obj) {
  const newObj = {};

  for (const prop in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, prop)) {
      newObj[prop] = obj[prop];
    }
  }

  return newObj;
}
```

这个函数只把第一层属性值赋给新对象。如果属性值本身是对象，复制过去的仍然是同一个引用。

## 2. Object.assign

`Object.assign()` 可以把一个或多个源对象的可枚举自有属性复制到目标对象：

```js
const obj = {
  age: 18,
  nature: ["smart", "good"],
  names: {
    name1: "fx",
    name2: "xka",
  },
  love() {
    console.log("fx is a great girl");
  },
};

const newObj = Object.assign({}, obj);
```

`age` 是基本值，会复制一份数值；`nature`、`names` 和 `love` 对应的引用会被直接复制，因此这仍然是浅拷贝。

```js
console.log(newObj !== obj); // true
console.log(newObj.names === obj.names); // true
console.log(newObj.nature === obj.nature); // true
```

## 3. 数组的浅拷贝

### 3.1 slice

```js
const source = ["One", "Two", "Three"];
const copy = source.slice(0);

copy[1] = "love";

console.log(source); // ["One", "Two", "Three"]
console.log(copy); // ["One", "love", "Three"]
```

### 3.2 concat

```js
const source = ["One", "Two", "Three"];
const copy = source.concat();

copy[1] = "love";

console.log(source); // ["One", "Two", "Three"]
console.log(copy); // ["One", "love", "Three"]
```

### 3.3 展开语法

```js
const source = ["One", "Two", "Three"];
const copy = [...source];

copy[1] = "love";

console.log(source); // ["One", "Two", "Three"]
console.log(copy); // ["One", "love", "Three"]
```

这些例子中的数组元素都是字符串，所以修改新数组的某个位置不会影响原数组。但如果元素是对象，内部对象仍然共享：

```js
const source = [{ name: "Tom" }];
const copy = [...source];

copy[0].name = "Lucy";

console.log(source[0].name); // "Lucy"
```

## 4. 浅拷贝的实际结果

```js
const obj1 = {
  name: "init",
  arr: [1, [2, 3], 4],
};

const obj3 = shallowClone(obj1);

obj3.name = "update";
obj3.arr[1] = [5, 6, 7];

console.log(obj1);
// { name: "init", arr: [1, [5, 6, 7], 4] }

console.log(obj3);
// { name: "update", arr: [1, [5, 6, 7], 4] }
```

最外层的 `name` 是独立的，但 `arr` 仍是同一个数组，所以通过副本修改嵌套数据会影响原对象。

## 5. 使用库进行深拷贝

### 5.1 Lodash cloneDeep

```js
const _ = require("lodash");

const obj1 = {
  a: 1,
  b: { f: { g: 1 } },
  c: [1, 2, 3],
};

const obj2 = _.cloneDeep(obj1);

console.log(obj1.b.f === obj2.b.f); // false
```

### 5.2 jQuery extend

资料还给出了 jQuery 的深拷贝写法：

```js
const $ = require("jquery");

const obj2 = $.extend(true, {}, obj1);

console.log(obj1.b.f === obj2.b.f); // false
```

第一个参数 `true` 表示递归合并。是否使用这两种方案，应看项目是否已经依赖对应库。

## 6. JSON 序列化深拷贝

```js
const obj2 = JSON.parse(JSON.stringify(obj1));
```

对于只包含 JSON 支持的数据，这种方式简单，但会丢失或改变某些值：

```js
const obj = {
  name: "A",
  name1: undefined,
  name3() {},
  name4: Symbol("A"),
};

const obj2 = JSON.parse(JSON.stringify(obj));

console.log(obj2); // { name: "A" }
```

对象属性中的 `undefined`、函数和 Symbol 不会出现在最终 JSON 中。它还不能直接处理循环引用，并会把 `Date` 等对象转换为不同表示。

## 7. 手写深拷贝

资料的递归实现使用 `WeakMap` 记录已经复制过的对象，从而处理循环引用：

```js
function deepClone(obj, hash = new WeakMap()) {
  if (obj === null) return obj;
  if (obj instanceof Date) return new Date(obj);
  if (obj instanceof RegExp) return new RegExp(obj);
  if (typeof obj !== "object") return obj;

  if (hash.has(obj)) return hash.get(obj);

  const cloneObj = Array.isArray(obj)
    ? []
    : Object.create(Object.getPrototypeOf(obj));

  hash.set(obj, cloneObj);

  for (const key of Reflect.ownKeys(obj)) {
    cloneObj[key] = deepClone(obj[key], hash);
  }

  return cloneObj;
}
```

`WeakMap` 必须在递归复制属性之前保存对应关系。否则对象直接或间接引用自身时，递归会无限继续。

```js
const source = { name: "cycle" };
source.self = source;

const copy = deepClone(source);

console.log(copy !== source); // true
console.log(copy.self === copy); // true
```

## 8. 深拷贝结果

```js
const obj1 = {
  name: "init",
  arr: [1, [2, 3], 4],
};

const obj4 = deepClone(obj1);

obj4.name = "update";
obj4.arr[1] = [5, 6, 7];

console.log(obj1);
// { name: "init", arr: [1, [2, 3], 4] }

console.log(obj4);
// { name: "update", arr: [1, [5, 6, 7], 4] }
```

新对象的嵌套数组已经独立，因此修改副本不会影响原对象。

## 9. 总结

1. 浅拷贝只复制第一层，嵌套对象仍共享引用。
2. `Object.assign()`、数组 `slice()`、`concat()` 和展开语法都是常见浅拷贝方式。
3. Lodash `cloneDeep()` 和 jQuery `extend(true, ...)` 可以递归复制。
4. JSON 方案只适合可安全序列化的数据，会丢失函数、`undefined`、Symbol 等内容。
5. 手写深拷贝需要处理特殊对象和循环引用；`WeakMap` 用于记录已复制对象。
