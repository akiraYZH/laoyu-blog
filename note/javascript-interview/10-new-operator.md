`new` 用来以构造方式调用函数。它不只是“调用一次函数”，还会创建对象、连接原型、绑定 `this`，并根据构造函数的返回值决定最终结果。

## 1. 基本用法

```js
function Person(name, age) {
  this.name = name;
  this.age = age;
}

Person.prototype.sayName = function () {
  console.log(this.name);
};

const person1 = new Person("Tom", 20);

console.log(person1); // Person { name: "Tom", age: 20 }
person1.sayName(); // "Tom"
```

执行 `new Person("Tom", 20)` 时，`Person` 内部的 `this` 指向新创建的实例，因此 `name` 和 `age` 被写到 `person1` 上。实例的原型又连接到 `Person.prototype`，所以能找到 `sayName()`。

## 2. new 做了什么

可以把 `new Constructor(...args)` 理解成四个主要步骤：

1. 创建一个新对象；
2. 把新对象的原型连接到构造函数的 `prototype`；
3. 以新对象作为 `this` 调用构造函数，并传入参数；
4. 根据构造函数的返回值决定返回新对象还是显式返回的对象。

关系可以表示为：

```text
Person 构造函数
    │
    │ prototype
    ▼
Person.prototype
    ▲
    │ [[Prototype]]
    │
person1 新实例
```

所以：

```js
Object.getPrototypeOf(person1) === Person.prototype; // true
```

## 3. 构造函数返回值规则

### 3.1 返回基本值

构造函数显式返回普通基本值时，这个返回值会被忽略，最终仍返回新创建的实例：

```js
function Test(name) {
  this.name = name;
  return 1;
}

const t = new Test("xxx");

console.log(t.name); // "xxx"
```

### 3.2 返回对象

如果构造函数显式返回对象，这个对象会替代原本的新实例：

```js
function Test(name) {
  this.name = name;
  console.log(this); // Test { name: "xxx" }
  return { age: 26 };
}

const t = new Test("xxx");

console.log(t); // { age: 26 }
console.log(t.name); // undefined
```

构造函数内部确实创建并修改了一个 `Test` 实例，但最终返回的是 `{ age: 26 }`，所以外部变量 `t` 接收到的不是那个实例。

### 3.3 返回 null

`null` 不会替代新实例：

```js
function Test(name) {
  this.name = name;
  return null;
}

const t = new Test("xxx");

console.log(t.name); // "xxx"
```

可以把返回值规则记成：

```text
显式返回非 null 对象或函数 → 返回该值
返回普通基本值、null，或没有 return → 返回新实例
```

## 4. 手写一个 myNew

按照前面的四个步骤，可以写出一个用于学习的近似实现：

```js
function myNew(Func, ...args) {
  // 1、2：创建对象，并连接 Func.prototype
  const obj = Object.create(Func.prototype);

  // 3：让构造函数内部的 this 指向 obj
  const result = Func.apply(obj, args);

  // 4：构造函数返回对象或函数时使用该返回值
  const isObject =
    result !== null &&
    (typeof result === "object" || typeof result === "function");

  return isObject ? result : obj;
}
```

资料中的写法是先创建空对象，再给 `__proto__` 赋值：

```js
const obj = {};
obj.__proto__ = Func.prototype;
```

这里使用 `Object.create(Func.prototype)` 表达同一个学习目标，同时避免直接修改 `__proto__`。

## 5. 验证 myNew

```js
function Person(name, age) {
  this.name = name;
  this.age = age;
}

Person.prototype.say = function () {
  console.log(this.name);
};

const p = myNew(Person, "huihui", 123);

console.log(p); // Person { name: "huihui", age: 123 }
p.say(); // "huihui"
```

逐步分析：

1. `Object.create(Person.prototype)` 创建 `obj`；
2. `Person.apply(obj, ["huihui", 123])` 把属性写到 `obj`；
3. `Person` 没有显式返回对象；
4. `myNew` 返回 `obj`；
5. `obj` 的原型是 `Person.prototype`，因此能调用 `say()`。

还可以验证原型关系：

```js
console.log(p instanceof Person); // true
console.log(Object.getPrototypeOf(p) === Person.prototype); // true
```

## 6. 验证返回对象的情况

```js
function CreateUser(name) {
  this.name = name;
  return { name: "returned object" };
}

const user = myNew(CreateUser, "original instance");

console.log(user.name); // "returned object"
console.log(user instanceof CreateUser); // false
```

因为构造函数明确返回了对象，`myNew` 按规则返回该对象。

## 7. 这个手写实现的边界

`myNew` 用来解释核心步骤很合适，但它不是 JavaScript `new` 的完整替代。例如：

- ES `class` 不能通过 `apply()` 当作普通函数执行；
- 规范中的构造过程还涉及 `new.target`；
- 构造函数的 `prototype` 不是对象时，还有额外的默认原型处理；
- 内建构造器可能具有普通函数无法模拟的内部行为。

需要通用构造能力时，可以使用：

```js
const person = Reflect.construct(Person, ["Tom", 20]);
```

## 8. 总结

1. `new` 创建一个新对象。
2. 新对象的原型连接到构造函数的 `prototype`。
3. 构造函数执行时，`this` 指向新对象。
4. 构造函数返回非 `null` 对象或函数时，该值成为最终结果。
5. 返回普通基本值、`null` 或没有返回值时，最终返回新实例。
6. `Object.create()` 加 `apply()` 可以近似展示 `new` 的核心过程，但不能完整替代语言内部构造语义。
