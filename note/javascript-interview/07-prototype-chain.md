JavaScript 常被称为“基于原型的语言”。要理解这句话，需要先分清两个概念：

- `prototype`：函数的原型属性；
- `__proto__`：对象指向其原型对象的连接。

多个对象通过这条连接一层层串起来，就形成了原型链。

## 一、什么是原型

JavaScript 中的函数也是对象，因此函数可以拥有属性。普通函数默认会有一个特殊属性 `prototype`：

```js
function doSomething() {}

console.log(doSomething.prototype);
```

输出的结构可以简化为：

```text
{
  constructor: doSomething,
  __proto__: Object.prototype
}
```

这里的 `doSomething.prototype` 就是 `doSomething` 函数的原型对象。

### prototype 与 constructor 的关系

原型对象中默认有一个 `constructor` 属性，它指回原来的函数：

```js
function doSomething() {}

console.log(doSomething.prototype.constructor === doSomething);
// true
```

可以把这两个对象之间的关系画成：

```text
doSomething
    │
    │ prototype
    ▼
doSomething.prototype
    │
    │ constructor
    └──────────────────▶ doSomething
```

也就是说：

- 函数通过 `.prototype` 找到自己的原型对象；
- 原型对象通过 `.constructor` 找回这个函数。

现在还没有创建任何实例。`prototype` 只是先准备好一个对象，将来通过 `new` 创建出来的实例可以连接到它。

## 二、什么是原型链

下面定义一个 `Person` 构造函数，并创建一个实例：

```js
function Person(name) {
  this.name = name;
  this.age = 18;
  this.sayName = function () {
    console.log(this.name);
  };
}

const person = new Person("person");
```

执行 `new Person("person")` 后，主要得到三个对象：

```text
Person                 构造函数
Person.prototype       Person 的原型对象
person                 通过 new 创建的实例对象
```

它们之间的关键关系是：

```js
person.__proto__ === Person.prototype;
// true
```

这句话表示：实例 `person` 的原型是 `Person.prototype`。

用图表示就是：

```text
Person
   │
   │ prototype
   ▼
Person.prototype
   ▲
   │ __proto__
   │
person
```

注意两条线的方向和名称：

- `Person.prototype`：从构造函数找到原型对象；
- `person.__proto__`：从实例找到原型对象。

它们最后都指向同一个对象 `Person.prototype`。

### 为什么实例能使用原型上的方法

把 `sayName` 放到原型对象上：

```js
function Person(name) {
  this.name = name;
  this.age = 18;
}

Person.prototype.sayName = function () {
  console.log(this.name);
};

const person = new Person("person");

person.sayName(); // "person"
```

`person` 自身只有 `name` 和 `age`，并没有自己的 `sayName`。当执行 `person.sayName()` 时，JavaScript 会这样查找：

```text
第一步：在 person 自身查找 sayName
        没找到

第二步：通过 person.__proto__ 找到 Person.prototype
        在 Person.prototype 上找到 sayName

第三步：调用找到的方法
```

对象自身找不到属性，就去它的原型对象上继续找。这种逐层查找关系就是原型链。

## 三、完整原型链

`Person.prototype` 本身也是一个普通对象。普通对象还会继续连接到 `Object.prototype`：

```js
Person.prototype.__proto__ === Object.prototype;
// true
```

而 `Object.prototype` 已经是这条原型链的最后一个对象，它的原型是 `null`：

```js
Object.prototype.__proto__ === null;
// true
```

所以实例 `person` 的完整原型链是：

```text
person
   │ __proto__
   ▼
Person.prototype
   │ __proto__
   ▼
Object.prototype
   │ __proto__
   ▼
null
```

这条链也解释了为什么普通对象可以调用 `toString()`、`hasOwnProperty()` 等方法：这些方法通常位于 `Object.prototype` 上。

```js
console.log(person.toString());
console.log(person.hasOwnProperty("name")); // true
```

JavaScript 先在 `person` 上查找。如果没有，就到 `Person.prototype`；如果仍然没有，再到 `Object.prototype`。查找到 `null` 时表示原型链结束。

如果整条链都没有目标属性，结果就是 `undefined`：

```js
console.log(person.notExist);
// undefined
```

## 四、构造函数本身也是函数对象

前面分析的是实例 `person` 的原型链。现在再看构造函数 `Person` 自身。

`Person` 是一个函数，而函数也是对象。普通函数由内置的 `Function` 构造器创建，因此：

```js
Person.__proto__ === Function.prototype;
// true
```

内置的 `Object` 也是函数对象，所以它同样连接到 `Function.prototype`：

```js
Object.__proto__ === Function.prototype;
// true
```

`Function.prototype` 自己又是一个对象，它会继续连接到 `Object.prototype`，最后到达 `null`：

```js
Function.prototype.__proto__ === Object.prototype;
Object.prototype.__proto__ === null;
```

因此可以把关系分成两条链理解。

### 实例对象的原型链

```text
person
  → Person.prototype
  → Object.prototype
  → null
```

### 构造函数对象的原型链

```text
Person
  → Function.prototype
  → Object.prototype
  → null
```

第一条链解释 `person` 如何找到实例方法；第二条链解释为什么函数 `Person` 可以使用 `call()`、`apply()`、`bind()` 等函数方法。

## 五、把 PDF 中的关系逐条读懂

### 1. 实例指向构造函数的原型对象

```js
person.__proto__ === Person.prototype;
```

`person` 是通过 `new Person()` 创建的，所以它连接到创建当时的 `Person.prototype`。

### 2. 构造函数指向 Function.prototype

```js
Person.__proto__ === Function.prototype;
```

`Person` 本身是函数对象，因此它的原型是 `Function.prototype`。

### 3. 构造函数的原型对象指向 Object.prototype

```js
Person.prototype.__proto__ === Object.prototype;
```

`Person.prototype` 是一个普通对象，所以它继续连接到 `Object.prototype`。

### 4. Object 构造函数也指向 Function.prototype

```js
Object.__proto__ === Function.prototype;
```

内置的 `Object` 可以被调用，也可以与 `new` 一起使用，因此它也是函数对象。

### 5. Object.prototype 的原型是 null

```js
Object.prototype.__proto__ === null;
```

`null` 表示原型链已经到达终点，不能再继续向上查找。

## 六、关于 `__proto__` 的说明

PDF 使用 `__proto__` 展示对象之间的关系，这种写法直观，适合理解图中的箭头。

在现代 JavaScript 中，读取对象原型更推荐使用：

```js
Object.getPrototypeOf(person) === Person.prototype;
```

它与下面这句在这里表达的是同一个关系：

```js
person.__proto__ === Person.prototype;
```

学习原型链时可以用 `__proto__` 帮助读图；编写正式代码时使用 `Object.getPrototypeOf()`，意图会更加清楚。

## 总结

1. 普通函数拥有 `.prototype` 属性，它指向函数的原型对象。
2. 原型对象默认拥有 `.constructor`，它指回对应的构造函数。
3. 使用 `new Person()` 创建实例时，实例的原型会连接到 `Person.prototype`。
4. 读取属性时，先查找对象自身，再沿原型逐层向上查找。
5. 实例链通常是 `person → Person.prototype → Object.prototype → null`。
6. 构造函数本身也是函数对象，因此 `Person → Function.prototype → Object.prototype → null`。

原型链不是一组需要死记的等式。先判断当前分析的是“实例对象”还是“构造函数对象”，再顺着 `__proto__` 一层层向上画，就能得到正确关系。

## 参考资料

- [MDN：继承与原型链](https://developer.mozilla.org/zh-CN/docs/Web/JavaScript/Inheritance_and_the_prototype_chain)
- [MDN：Object.getPrototypeOf()](https://developer.mozilla.org/zh-CN/docs/Web/JavaScript/Reference/Global_Objects/Object/getPrototypeOf)
