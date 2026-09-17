大文件直接一次上传，容易受到网络中断、请求超时和重试成本影响。常见方案是把文件切成多个分片逐个上传，并用文件标识和分片序号记录进度，从而实现暂停、续传和秒传判断。

## 1. 整体流程

资料中的大文件上传流程可以整理为：

1. 用户选择文件；
2. 计算文件 MD5，得到文件标识；
3. 校验文件类型；
4. 把文件切成固定大小的分片；
5. 上传每个分片并记录进度；
6. 服务器检查已上传分片，支持断点续传；
7. 全部分片完成后，请求服务器合并文件；
8. 需要暂停时终止正在进行的 XHR。

```text
File → MD5/类型检查 → Slice 分片 → 分片上传 → 服务端合并
```

## 2. 选择文件

```html
<input type="file" id="file-input" />
```

```js
const input = document.querySelector("#file-input");

input.addEventListener("change", function () {
  const file = this.files[0];

  if (!file) return;

  console.log(file.name, file.size, file.type);
});
```

`File` 对象包含文件名、大小、MIME 类型等信息，也继承了 `Blob` 的切片能力。

## 3. 计算文件标识

资料使用 MD5 作为文件标识：

```js
const md5Code = md5(file);
```

实际计算通常需要分块读取文件内容，避免一次把超大文件全部放进内存。文件标识可以用于：

- 询问服务器文件是否已经存在；
- 标识属于同一个文件的所有分片；
- 查询服务器已收到哪些分片；
- 合并时确认目标文件。

> 说明：MD5 在这里用于内容标识和重复检测，不应用作密码或安全签名算法。客户端哈希也不能替代服务器端的完整性校验。

## 4. 使用 FileReader 读取文件

资料展示了把文件读取为 `ArrayBuffer`，再截取前 10MB：

```js
const reader = new FileReader();

reader.readAsArrayBuffer(file);

reader.addEventListener("load", function (event) {
  const firstChunk = event.target.result.slice(0, 10 * 1024 * 1024);
  console.log(firstChunk.byteLength);
});
```

现代代码也可以直接对 `File` 使用 `slice()`，不必先读取整个文件：

```js
const chunkSize = 10 * 1024 * 1024;
const firstChunk = file.slice(0, chunkSize);
```

## 5. 切分全部分片

```js
function createChunks(file, chunkSize = 10 * 1024 * 1024) {
  const chunks = [];

  for (let start = 0; start < file.size; start += chunkSize) {
    chunks.push({
      index: chunks.length,
      blob: file.slice(start, start + chunkSize),
    });
  }

  return chunks;
}
```

```js
const chunks = createChunks(file);

console.log(chunks.length);
```

每个分片需要带上至少这些信息：文件标识、分片索引、总分片数和分片数据。

## 6. 使用 FormData 和 XMLHttpRequest 上传

资料使用 `FormData` 发送分片：

```js
function uploadChunk({ fileId, fileName, chunk, total }) {
  const formData = new FormData();

  formData.append("chunk", chunk.blob);
  formData.append("fileId", fileId);
  formData.append("filename", fileName);
  formData.append("index", String(chunk.index));
  formData.append("total", String(total));

  const xhr = new XMLHttpRequest();
  xhr.open("POST", "/api/upload/chunk");
  xhr.send(formData);

  return xhr;
}
```

资料原例以 `0` 作为分片字段名，并附加文件名：

```js
const formData = new FormData();
formData.append("0", slice);
formData.append("filename", file.name);
```

字段名和接口地址应以服务端约定为准。

## 7. 上传进度

上传进度应监听 `xhr.upload` 的 `progress` 事件：

```js
xhr.upload.addEventListener("progress", updateProgress);

function updateProgress(event) {
  if (!event.lengthComputable) return;

  const percent = (event.loaded / event.total) * 100;
  console.log(`${percent.toFixed(2)}%`);
}
```

`xhr` 自身的 `progress` 更接近响应下载进度，而 `xhr.upload` 表示请求体上传进度。

## 8. 校验文件类型

只看 `file.type` 或文件扩展名并不完全可靠。资料进一步读取文件开头的字节，也就是文件签名（Magic Number），用于识别 JPG、PNG、MP4、M4V、MOV、OGG、AVI 等格式。

例如：

```text
JPG：FF D8 FF
PNG：89 50 4E
OGG：4F 67 67 53
```

读取开头字节：

```js
async function readSignature(file, byteLength = 12) {
  const buffer = await file.slice(0, byteLength).arrayBuffer();

  return [...new Uint8Array(buffer)]
    .map((value) => value.toString(16).padStart(2, "0"))
    .join(" ");
}
```

简单判断图片：

```js
async function detectImageType(file) {
  const signature = await readSignature(file, 3);

  if (signature.startsWith("ff d8 ff")) return "jpg";
  if (signature.startsWith("89 50 4e")) return "png";

  return false;
}
```

资料还使用更长的签名区分 MP4、MOV、M4V、OGG、AVI，因为这些容器格式的关键字节可能位于文件头不同位置。

```js
const fileType = await detectImageType(file);

if (!fileType) {
  console.log("文件类型不符合要求");
}
```

> 说明：客户端校验只用于尽早提示用户，不能代替服务器再次验证扩展名、MIME、文件签名、大小和内容安全。

## 9. 生成服务器文件名

资料把 MD5 与识别出的扩展名组合：

```js
formData.append("filename", `${md5Code}.${fileType}`);
```

这样同一文件可以得到稳定标识，但服务器仍应自己决定最终存储名称，避免信任客户端输入。

## 10. 暂停与断点续传

`XMLHttpRequest.abort()` 可以终止当前请求：

```js
let currentRequest = uploadChunk(options);

function pauseUpload() {
  currentRequest?.abort();
}
```

暂停后，客户端需要保存尚未上传的分片索引。恢复时先向服务器查询已存在的分片，只上传缺少部分：

```js
async function resumeUpload(fileId, chunks) {
  const response = await fetch(`/api/upload/status?fileId=${fileId}`);
  const { uploadedIndexes } = await response.json();
  const uploaded = new Set(uploadedIndexes);

  return chunks.filter((chunk) => !uploaded.has(chunk.index));
}
```

断点续传不是只靠前端 `abort()` 完成。服务器必须持久化分片状态，并保证重复上传同一分片不会破坏结果。

## 11. 合并文件

所有分片上传成功后，再请求服务器按索引合并：

```js
await fetch("/api/upload/merge", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    fileId: md5Code,
    filename: `${md5Code}.${fileType}`,
    total: chunks.length,
  }),
});
```

服务器合并前应确认所有分片齐全，并按正确顺序写入目标文件。

## 12. 总结

1. 大文件上传先生成文件标识，再切成固定大小分片。
2. `File.slice()` 负责切片，`FormData` 与 XHR 可发送分片。
3. 上传进度监听 `xhr.upload.progress`。
4. 文件类型可以结合扩展名、MIME 和文件签名判断，但服务端必须再次验证。
5. `abort()` 只能暂停当前请求；真正的断点续传还需要服务端记录已上传分片。
6. 全部分片到齐后，由服务器按索引合并并校验完整性。
