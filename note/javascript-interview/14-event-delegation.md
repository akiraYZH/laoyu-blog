事件委托利用事件冒泡，把多个子元素的事件统一交给共同的父元素处理。它适合 `click`、`mousedown`、`mouseup`、`keydown`、`keyup`、`keypress` 等能够冒泡的事件。

## 1. 为什么需要事件委托

假设列表中有很多 `li`：

```html
<ul id="list">
  <li>item 1</li>
  <li>item 2</li>
  <li>item 3</li>
  <!-- ... -->
  <li>item n</li>
</ul>
```

不使用事件委托时，可以遍历所有 `li`，分别绑定事件：

```js
const items = document.getElementsByTagName("li");

for (let i = 0; i < items.length; i++) {
  items[i].onclick = function (event) {
    console.log(event.target.innerHTML);
  };
}
```

这种方式的问题是：

- 子元素很多时，需要创建很多处理函数；
- 后续动态添加的新 `li` 没有自动绑定事件；
- 统一移除、修改行为不方便。

## 2. 事件委托的原理

点击 `li` 后，`click` 事件会向父元素 `ul` 冒泡。因此可以只在 `ul` 上注册一个监听器，再根据 `event.target` 判断具体点击了哪个子元素。

```js
const list = document.getElementById("list");

list.addEventListener("click", function (event) {
  const target = event.target;

  if (target.nodeName.toLowerCase() === "li") {
    console.log("the content is:", target.innerHTML);
  }
});
```

整个过程是：

```text
用户点击 li
  ↓
事件从 li 向 ul 冒泡
  ↓
ul 的监听器执行
  ↓
通过 event.target 找到实际点击的 li
```

> 说明：资料示例把 `toLocaleLowerCase` 当作属性比较，少了括号。正确写法是调用 `toLowerCase()` 或 `toLocaleLowerCase()`。

## 3. 动态添加元素

事件委托的一个重要优点，是后续加入父元素的新子节点也能使用相同行为。

```html
<input type="button" id="btn" value="添加" />

<ul id="ul1">
  <li>item 1</li>
  <li>item 2</li>
  <li>item 3</li>
  <li>item 4</li>
</ul>
```

```js
const button = document.getElementById("btn");
const list = document.getElementById("ul1");
let num = 4;

list.onclick = function (event) {
  const target = event.target;

  if (target.nodeName.toLowerCase() === "li") {
    console.log("the content is:", target.innerHTML);
  }
};

button.onclick = function () {
  num++;

  const item = document.createElement("li");
  item.innerHTML = `item ${num}`;
  list.appendChild(item);
};
```

新创建的 `li` 没有单独绑定 `onclick`，但点击它时事件仍然会冒泡到 `ul1`，因此父元素的处理程序照样生效。

## 4. event.target 与 currentTarget

事件委托中必须分清：

- `event.target`：用户真正点击的元素；
- `event.currentTarget`：绑定监听器的父元素。

```js
list.addEventListener("click", function (event) {
  console.log(event.target); // 被点击的 li 或其内部元素
  console.log(event.currentTarget); // ul#ul1
});
```

如果 `li` 内部还有 `span`，用户点击文字时，`target` 可能是 `span` 而不是 `li`：

```html
<ul id="list">
  <li data-id="1"><span>item 1</span></li>
</ul>
```

这时只检查 `target.nodeName === "LI"` 会漏掉点击。可以使用 `closest()` 找到真正需要处理的子元素：

```js
list.addEventListener("click", function (event) {
  const item = event.target.closest("li");

  if (!item || !list.contains(item)) return;

  console.log(item.dataset.id);
});
```

`list.contains(item)` 防止 `closest()` 找到委托容器之外的同类祖先元素。

## 5. 哪些事件适合委托

能够冒泡的事件通常适合事件委托，例如：

- `click`；
- `mousedown`、`mouseup`；
- `keydown`、`keyup`、`keypress`；
- `input`、`change`（现代浏览器中常见场景）。

有些事件不按普通方式冒泡，例如资料列出的：

- `focus`；
- `blur`；
- `mouseenter`、`mouseleave`；
- `mousemove`、`mouseout` 虽可传播，但触发频繁或边界复杂，委托时要谨慎。

对于焦点委托，可以使用会冒泡的 `focusin`、`focusout`，或者在捕获阶段监听 `focus`、`blur`：

```js
form.addEventListener(
  "focus",
  (event) => {
    event.target.classList.add("focused");
  },
  true,
);
```

## 6. 事件委托的优点

1. 减少监听器数量：父元素只需一个监听器；
2. 支持动态子元素：后来插入的元素不需要重新绑定；
3. 统一管理逻辑：添加、修改和移除处理程序更集中；
4. 适合大型列表、表格、菜单等重复结构。

## 7. 使用边界

事件委托也不是所有场景都适合：

- 事件必须能够到达委托元素；
- 内部代码调用 `stopPropagation()` 后，父元素可能收不到事件；
- 子元素结构复杂时，应使用 `closest()` 确认真正的业务目标；
- 高频事件可能让父容器处理大量无关触发，需要做范围判断；
- 不要直接使用不可信内容的 `innerHTML`，读取纯文本时可使用 `textContent`。

## 8. 总结

1. 事件委托建立在事件冒泡之上。
2. 在共同父元素注册一个监听器，通过 `event.target` 找到实际目标。
3. 它可以减少监听器数量，并自动支持后续动态添加的子元素。
4. 子元素内部还有嵌套标签时，使用 `closest()` 比只判断 `nodeName` 更稳妥。
5. `focus`、`blur` 等不按普通方式冒泡的事件，需要使用对应的冒泡事件或捕获阶段。
