JavaScript 继承的核心是让子对象复用父对象的属性和方法。ES6 `class` 与 `extends` 让语法更接近传统面向对象语言，但底层仍然建立在原型链之上。

## 1. class 与 extends

先定义父类：

```js
class Car {
  constructor(color, speed) {
    this.color = color;
    this.speed = speed;
  }
}
```

卡车继承汽车：

```js
class Truck extends Car {
  constructor(color, speed) {
    super(color, speed);
    this.hasContainer = true;
  }
}
```

`super(color, speed)` 调用父类构造函数，把 `color` 和 `speed` 初始化到当前实例。子类还可以覆盖父类初始化的值：

```js
class Truck extends Car {
  constructor(color, speed) {
    super(color, speed);
    this.color = "black";
    this.hasContainer = true;
  }
}
```

派生类构造函数在访问 `this` 前必须先调用 `super()`。

下面继续按资料顺序理解 ES5 中的各种继承方式，以及它们分别解决了什么问题。

## 2. 原型链继承

```js
function Parent() {
  this.name = "parent1";
  this.play = [1, 2, 3];
}

function Child() {
  this.type = "child";
}

Child.prototype = new Parent();
Child.prototype.constructor = Child;
```

`Child.prototype` 被设置为一个 `Parent` 实例，因此子实例可以沿原型链找到父属性和方法：

```js
const child = new Child();

console.log(child.name); // "parent1"
```

问题是：父构造函数创建的引用类型属性位于共享原型对象上。

```js
const child1 = new Child();
const child2 = new Child();

child1.play.push(4);

console.log(child1.play); // [1, 2, 3, 4]
console.log(child2.play); // [1, 2, 3, 4]
```

两个实例共享同一个 `play` 数组，修改一个会影响另一个。

## 3. 构造函数借用

在子构造函数中用 `call()` 执行父构造函数：

```js
function Parent() {
  this.name = "parent1";
  this.play = [1, 2, 3];
}

Parent.prototype.getName = function () {
  return this.name;
};

function Child() {
  Parent.call(this);
  this.type = "child";
}
```

每次 `new Child()` 都会在新实例上创建独立的 `name` 和 `play`：

```js
const child1 = new Child();
const child2 = new Child();

child1.play.push(4);

console.log(child1.play); // [1, 2, 3, 4]
console.log(child2.play); // [1, 2, 3]
```

但只调用 `Parent.call(this)` 没有建立 `Child.prototype → Parent.prototype` 的连接，因此：

```js
// child1.getName(); // TypeError
```

子实例无法继承父原型上的 `getName()`。

## 4. 组合继承

组合继承同时使用构造函数借用和原型链继承：

```js
function Parent3() {
  this.name = "parent3";
  this.play = [1, 2, 3];
}

Parent3.prototype.getName = function () {
  return this.name;
};

function Child3() {
  Parent3.call(this);
  this.type = "child3";
}

Child3.prototype = new Parent3();
Child3.prototype.constructor = Child3;
```

```js
const child3 = new Child3();
const child4 = new Child3();

child3.play.push(4);

console.log(child3.play); // [1, 2, 3, 4]
console.log(child4.play); // [1, 2, 3]
console.log(child3.getName()); // "parent3"
console.log(child4.getName()); // "parent3"
```

它既让实例拥有独立引用属性，又能继承父原型方法。缺点是父构造函数执行两次：一次创建子原型，一次创建子实例。

## 5. 原型式继承

原型式继承直接以一个现有对象作为新对象的原型，`Object.create()` 就是标准实现。

```js
const parent4 = {
  name: "parent4",
  friends: ["p1", "p2", "p3"],
  getName() {
    return this.name;
  },
};

const person4 = Object.create(parent4);
person4.name = "tom";
person4.friends.push("jerry");

const person5 = Object.create(parent4);
person5.friends.push("lucy");
```

```js
console.log(person4.name); // "tom"
console.log(person4.name === person4.getName()); // true
console.log(person5.name); // "parent4"

console.log(person4.friends);
console.log(person5.friends);
// 都是 ["p1", "p2", "p3", "jerry", "lucy"]
```

因为 `friends` 位于共享的 `parent4` 上，两个对象仍然会共享该数组。

## 6. 寄生式继承

寄生式继承先创建一个继承原对象的新对象，再为它增加方法或属性：

```js
const parent5 = {
  name: "parent5",
  friends: ["p1", "p2", "p3"],
  getName() {
    return this.name;
  },
};

function clone(original) {
  const cloneObject = Object.create(original);

  cloneObject.getFriends = function () {
    return this.friends;
  };

  return cloneObject;
}

const person5 = clone(parent5);

console.log(person5.getName()); // "parent5"
console.log(person5.getFriends()); // ["p1", "p2", "p3"]
```

它适合在现有对象基础上快速增强，但每次调用都会重新创建增强方法，而且引用属性仍可能共享。

## 7. 寄生组合式继承

寄生组合式继承避免通过 `new Parent()` 创建子原型，而是直接用 `Object.create()` 连接原型：

```js
function inherit(Parent, Child) {
  Child.prototype = Object.create(Parent.prototype);
  Child.prototype.constructor = Child;
}

function Parent6() {
  this.name = "parent6";
  this.play = [1, 2, 3];
}

Parent6.prototype.getName = function () {
  return this.name;
};

function Child6() {
  Parent6.call(this);
  this.friends = "child6";
}

inherit(Parent6, Child6);

Child6.prototype.getFriends = function () {
  return this.friends;
};
```

```js
const person6 = new Child6();

console.log(person6.name); // "parent6"
console.log(person6.play); // [1, 2, 3]
console.log(person6.getName()); // "parent6"
console.log(person6.getFriends()); // "child6"
```

这里父构造函数只在创建子实例时执行一次，同时保留父原型方法，是 ES5 继承中较完整的组合方式。

## 8. ES6 extends 的原型关系

```js
class Person {
  constructor(name) {
    this.name = name;
  }

  getName() {
    console.log("Person:", this.name);
  }
}

class Gamer extends Person {
  constructor(name, age) {
    super(name);
    this.age = age;
  }
}

const asuna = new Gamer("Asuna", 20);
asuna.getName(); // "Person: Asuna"
```

类方法 `getName()` 位于 `Person.prototype`。`extends` 建立了类似寄生组合继承的原型关系：

```js
Object.getPrototypeOf(Gamer.prototype) === Person.prototype; // true
Object.getPrototypeOf(Gamer) === Person; // true
```

> 说明：资料把 `getName = function () {}` 与原型方法放在一起说明。标准的 `getName() {}` 是原型方法；类字段写法 `getName = function () {}` 通常会为每个实例创建属性，两者存放位置不同。

## 9. 总结

1. 原型链继承能继承父原型，但引用类型属性会被实例共享。
2. 构造函数借用让每个实例拥有独立属性，但单独使用无法继承父原型方法。
3. 组合继承解决两者问题，却会执行两次父构造函数。
4. `Object.create()` 实现原型式继承，但原型上的引用属性仍共享。
5. 寄生组合式继承通过 `Parent.call(this)` 和 `Object.create(Parent.prototype)` 组合优点。
6. `class extends` 提供更清晰语法，底层仍然建立原型链关系。
