DOM（Document Object Model，文档对象模型）把 HTML 或 XML 文档表示成由节点组成的树。JavaScript 可以通过 DOM API 查询、创建、更新、插入和删除页面节点。

## 1. DOM 节点

在 HTML 文档中，常见节点包括元素节点、文本节点和属性节点。例如：

```html
<p id="message">Hello</p>
```

这里包含一个 `p` 元素节点、一个 `id` 属性，以及一个内容为 `Hello` 的文本节点。DOM 的常见操作可以分为五类：创建、查询、更新、添加和删除。

## 2. 创建节点

### 2.1 createElement

`document.createElement()` 创建元素节点：

```js
const div = document.createElement("div");
div.className = "card";
```

此时元素只存在于内存中，还没有出现在页面上。必须把它插入文档：

```js
document.body.appendChild(div);
```

### 2.2 createTextNode

`document.createTextNode()` 创建文本节点：

```js
const paragraph = document.createElement("p");
const text = document.createTextNode("这是一段文字");

paragraph.appendChild(text);
document.body.appendChild(paragraph);
```

### 2.3 DocumentFragment

`DocumentFragment` 是一个轻量的临时容器。可以先把多个节点放入片段，再一次插入页面：

```js
const fragment = document.createDocumentFragment();

for (let i = 1; i <= 3; i++) {
  const item = document.createElement("li");
  item.textContent = `第 ${i} 项`;
  fragment.appendChild(item);
}

document.querySelector("ul").appendChild(fragment);
```

插入时，真正进入文档的是片段中的子节点，`DocumentFragment` 本身不会成为页面结构的一部分。

### 2.4 createAttribute

`document.createAttribute()` 可以创建属性节点，再通过 `setAttributeNode()` 添加到元素：

```js
const button = document.createElement("button");
const typeAttribute = document.createAttribute("type");

typeAttribute.value = "button";
button.setAttributeNode(typeAttribute);
```

实际开发中，更常见的写法是直接使用 `setAttribute()`：

```js
button.setAttribute("type", "button");
```

## 3. 查询节点

### 3.1 querySelector 与 querySelectorAll

`querySelector()` 返回第一个匹配 CSS 选择器的元素：

```js
const firstCard = document.querySelector(".card");
const submitButton = document.querySelector("form button[type='submit']");
```

`querySelectorAll()` 返回所有匹配元素组成的静态 `NodeList`：

```js
const cards = document.querySelectorAll(".card");

cards.forEach((card) => {
  console.log(card.textContent);
});
```

所谓静态，是指之后 DOM 发生变化时，已经取得的这个 `NodeList` 不会自动同步新增或删除的元素。

### 3.2 传统查询方法

```js
const app = document.getElementById("app");
const cards = document.getElementsByClassName("card");
const paragraphs = document.getElementsByTagName("p");
const options = document.getElementsByName("language");
```

其中 `getElementsByClassName()` 和 `getElementsByTagName()` 返回的通常是动态集合，文档变化后，集合内容也会随之变化。

### 3.3 文档快捷属性

```js
console.log(document.documentElement); // <html>
console.log(document.body); // <body>
console.log(document.all); // 文档中的全部元素集合（历史 API）
```

`document.all` 属于历史兼容 API，现代代码通常使用标准选择器方法。

### 3.4 节点关系

查询到一个节点后，还可以沿着 DOM 树访问它的亲属节点：

```js
const current = document.querySelector(".current");

console.log(current.parentNode);
console.log(current.childNodes);
console.log(current.firstChild);
console.log(current.lastChild);
console.log(current.nextSibling);
console.log(current.previousSibling);
```

这些属性返回的是“节点”，因此可能包含文本节点。如果只需要元素节点，可以使用：

```js
console.log(current.children);
console.log(current.firstElementChild);
console.log(current.lastElementChild);
console.log(current.nextElementSibling);
console.log(current.previousElementSibling);
```

## 4. 更新节点

### 4.1 innerHTML

`innerHTML` 读取或替换元素内部的 HTML：

```js
const container = document.querySelector("#container");
container.innerHTML = "<strong>Hello</strong>";
```

> 说明：不要把未经处理的用户输入直接赋给 `innerHTML`，否则可能产生 XSS 风险。只写入纯文本时优先使用 `textContent`。

### 4.2 innerText 与 textContent

```js
const message = document.querySelector("#message");

message.innerText = "用户可以看到的文字";
message.textContent = "节点中的文本内容";
```

- `innerText` 更接近页面渲染后可见的文本，会考虑样式和换行；
- `textContent` 读取节点内部的文本内容，不需要根据页面布局计算可见性。

### 4.3 style

CSS 中带连字符的属性，在 JavaScript 中通常写成驼峰形式：

```js
const box = document.querySelector(".box");

box.style.color = "white";
box.style.backgroundColor = "black";
box.style.fontSize = "18px";
```

## 5. 添加节点

### 5.1 使用 innerHTML

直接赋值会替换元素原有内容：

```js
const list = document.querySelector("#list");
list.innerHTML = "<li>新的唯一内容</li>";
```

如果希望在原有字符串后追加，可以写：

```js
list.innerHTML += "<li>追加内容</li>";
```

不过这种方式会重新解析和创建内部节点。需要保留已有节点及其状态时，更适合使用节点插入 API。

### 5.2 appendChild

`appendChild()` 把节点添加到父节点末尾：

```js
const list = document.querySelector("#list");
const item = document.createElement("li");

item.textContent = "新项目";
list.appendChild(item);
```

如果被添加的节点已经位于文档中，`appendChild()` 会把它移动到新位置，而不是复制一个新节点：

```js
const firstList = document.querySelector("#first-list");
const secondList = document.querySelector("#second-list");
const item = firstList.querySelector("li");

secondList.appendChild(item); // 从 firstList 移到 secondList
```

### 5.3 insertBefore

`insertBefore(newNode, referenceNode)` 在参考节点之前插入新节点：

```js
const list = document.querySelector("#list");
const firstItem = list.firstElementChild;
const newItem = document.createElement("li");

newItem.textContent = "插入到最前面";
list.insertBefore(newItem, firstItem);
```

### 5.4 setAttribute

`setAttribute()` 添加或更新元素属性：

```js
const link = document.querySelector("a");

link.setAttribute("href", "/articles");
link.setAttribute("title", "查看全部文章");
```

## 6. 删除节点

传统 DOM API 通过父节点调用 `removeChild()`：

```js
const list = document.querySelector("#list");
const item = list.querySelector("li");

const removedItem = list.removeChild(item);
```

节点虽然从文档中移除了，但如果变量仍然引用它，它还可以再次使用：

```js
document.querySelector("#other-list").appendChild(removedItem);
```

现代浏览器也支持元素自己的 `remove()` 方法：

```js
document.querySelector(".ad").remove();
```

## 7. 一个完整示例

```html
<ul id="todo-list"></ul>
```

```js
const list = document.querySelector("#todo-list");
const fragment = document.createDocumentFragment();

["学习 DOM", "完成练习", "整理笔记"].forEach((text) => {
  const item = document.createElement("li");
  item.textContent = text;
  item.setAttribute("data-status", "pending");
  fragment.appendChild(item);
});

list.appendChild(fragment);

const firstItem = list.firstElementChild;
firstItem.style.color = "green";
firstItem.setAttribute("data-status", "done");

const lastItem = list.lastElementChild;
list.removeChild(lastItem);
```

这个示例依次使用了查询、创建、属性更新、样式更新、插入和删除操作。

## 8. 总结

- DOM 把文档表示为节点树。
- `createElement()`、`createTextNode()` 和 `DocumentFragment` 用于创建节点。
- `querySelector()` 和 `querySelectorAll()` 支持 CSS 选择器查询。
- `innerHTML`、`innerText`、`textContent` 和 `style` 可以更新内容或样式。
- `appendChild()` 会插入或移动节点，`insertBefore()` 可以指定插入位置。
- `removeChild()` 删除节点后，只要变量仍保存引用，该节点仍然可以再次使用。
