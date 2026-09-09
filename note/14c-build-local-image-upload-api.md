---
title: "为 Markdown 编辑器建立本地图片上传 API"
description: "使用 IFormFile、独立图片存储 Service、wwwroot Static Files 和 Docker Volume，实现单张图片上传与公开访问。"
tags:
  - ASP.NET Core
  - File Upload
  - IFormFile
  - Static Files
  - Docker Volume
---

# 为 Markdown 编辑器建立本地图片上传 API

Markdown 正文只需要保存图片 URL，而图片文件必须先上传到可访问的存储位置。本文实现单张图片上传：Controller 接收 `multipart/form-data`，Service 验证并保存文件，Static Files Middleware 负责通过 URL 返回图片。

当前方案面向本地开发和学习项目。生产环境通常应使用 Object Storage，并增加文件签名检测、恶意内容扫描和访问策略。

## 请求和存储流程

```text
Markdown Editor 选择一张图片
    ↓ multipart/form-data
POST /api/images
    ↓
UploadsController
    ↓
IImageStorageService
    ↓
wwwroot/uploads/<随机文件名>
    ↓
返回 /uploads/<随机文件名>
    ↓
Markdown 使用返回的 `/uploads/...` URL 插入图片
```

## 为什么需要 Service

Controller 负责 HTTP Binding 和 Status Code。以下规则属于文件存储工作流：

```text
文件是否为空
大小是否超限
扩展名是否允许
如何生成安全文件名
保存到哪里
返回什么 URL
```

把它们放入 Service 后，未来把本地磁盘替换为 S3 或其他 Object Storage 时，不需要重写 Controller。

## 定义上传结果

新建 `Services/ImageStorage/ImageUploadResult.cs`：

```csharp
namespace BlogApi.Services;

public sealed class ImageUploadResult
{
    public bool Succeeded { get; init; }
    public string? Url { get; init; }
    public string? ErrorMessage { get; init; }

    public static ImageUploadResult Success(string url) =>
        new() { Succeeded = true, Url = url };

    public static ImageUploadResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
```

Service Result 表达存储操作成功或可预期的验证失败；Controller 再把它转换成 HTTP Response。

## 定义存储接口

新建 `Services/ImageStorage/IImageStorageService.cs`：

```csharp
namespace BlogApi.Services;

public interface IImageStorageService
{
    Task<ImageUploadResult> SaveAsync(
        IFormFile file,
        CancellationToken cancellationToken);
}
```

Interface 和实现放在同一个 Feature Folder 是合理的，因为它们共同描述图片存储能力。

## 实现本地存储

新建 `Services/ImageStorage/LocalImageStorageService.cs`。核心配置：

```csharp
private const string UploadsFolder = "uploads";
private const long MaxFileSize = 5 * 1024 * 1024;

private static readonly HashSet<string> AllowedExtensions =
[
    ".jpg",
    ".jpeg",
    ".png",
    ".gif",
    ".webp"
];
```

保存方法：

```csharp
public async Task<ImageUploadResult> SaveAsync(
    IFormFile file,
    CancellationToken cancellationToken)
{
    if (file.Length == 0)
    {
        return ImageUploadResult.Failure(
            "Please select an image to upload.");
    }

    if (file.Length > MaxFileSize)
    {
        return ImageUploadResult.Failure(
            "Image size cannot exceed 5 MB.");
    }

    var extension = Path
        .GetExtension(file.FileName)
        .ToLowerInvariant();

    if (!AllowedExtensions.Contains(extension))
    {
        return ImageUploadResult.Failure(
            "Only JPG, PNG, GIF, and WebP images are supported.");
    }

    var fileName = $"{Guid.NewGuid():N}{extension}";

    var webRootPath = _environment.WebRootPath;

    if (string.IsNullOrWhiteSpace(webRootPath))
    {
        webRootPath = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot");
    }

    var uploadsDirectory = Path.Combine(
        webRootPath,
        UploadsFolder);

    Directory.CreateDirectory(uploadsDirectory);

    var filePath = Path.Combine(
        uploadsDirectory,
        fileName);

    await using var stream = new FileStream(
        filePath,
        FileMode.CreateNew,
        FileAccess.Write,
        FileShare.None,
        bufferSize: 81920,
        useAsync: true);

    await file.CopyToAsync(stream, cancellationToken);

    return ImageUploadResult.Success(
        $"/{UploadsFolder}/{fileName}");
}
```

不要直接使用客户端提供的 `file.FileName` 作为磁盘文件名。使用服务器生成的随机名称可以避免路径字符、覆盖现有文件和名称冲突。

`FileStream` 参数含义：

```text
FileMode.CreateNew → 只创建新文件，已存在时失败
FileAccess.Write   → 只写入
FileShare.None     → 写入期间不与其他操作共享
bufferSize         → Stream 的缓冲区大小
useAsync: true     → 使用异步文件 I/O
await using        → 异步操作结束后释放 Stream
```

## 创建 Controller

新建 `Controllers/UploadsController.cs`：

```csharp
[ApiController]
[Route("api")]
public sealed class UploadsController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;

    public UploadsController(
        IImageStorageService imageStorageService)
    {
        _imageStorageService = imageStorageService;
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadImageResponseDto>>
        UploadImage(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken)
    {
        var result = await _imageStorageService.SaveAsync(
            file,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage
            });
        }

        return Ok(new UploadImageResponseDto
        {
            Url = result.Url!
        });
    }
}
```

Response DTO 只需要：

```csharp
public sealed class UploadImageResponseDto
{
    public required string Url { get; init; }
}
```

接口只接收一个 `IFormFile`，因此一次 Request 只处理一张图片。

## 注册 Service 和 Static Files

在 `Program.cs` 注册接口与实现：

```csharp
builder.Services.AddScoped<
    IImageStorageService,
    LocalImageStorageService>();
```

如果忘记注册，创建 Controller 时会出现：

```text
Unable to resolve service for type IImageStorageService
```

在 Pipeline 中启用 Static Files：

```csharp
app.UseStaticFiles();
```

这让 `wwwroot/uploads/example.png` 可以通过：

```text
/uploads/example.png
```

访问。

## Docker 中持久化上传目录

Container Filesystem 会随 Container 重建而消失。Compose 中加入 Bind Mount：

```yaml
services:
  api:
    volumes:
      - ./wwwroot/uploads:/src/wwwroot/uploads
```

这样文件实际保存在 Host 的：

```text
wwwroot/uploads
```

开发 Watch 还应忽略上传目录，避免每次新增图片触发 `dotnet watch` 重启：

```yaml
develop:
  watch:
    - action: sync
      ignore:
        - wwwroot/uploads/
```

## 验证

使用 `multipart/form-data` 发送字段名为 `file` 的单张图片：

```bash
curl -F "file=@sample.png" \
  http://localhost:8080/api/images
```

成功响应示例：

```json
{
  "url": "/uploads/random-name.png"
}
```

再访问：

```http
GET http://localhost:8080/uploads/random-name.png
```

应返回图片内容。

## 常见错误

### 前端使用 api:8080 作为图片 URL

`api` 是 Docker Network 内部 Hostname，浏览器无法解析。后端返回相对 URL `/uploads/...`，由开发 Proxy 或同域部署负责转发。

### 图片存在于 Container，但 Codebase 看不到

没有 Volume 时，文件只写进 Container 的 `/src/wwwroot/uploads`。加入 Bind Mount 后，Host Codebase 才会出现对应文件。

### 新图片触发后端重启

`dotnet watch` 监控到 `wwwroot/uploads` 新文件。把上传目录加入 Compose Watch Ignore。

### 只验证扩展名

扩展名来自客户端，不能证明文件内容真实。当前实现足够用于本地学习；生产环境还应检查文件 Signature/Magic Bytes，限制 Image Dimensions，并评估 Malware Scan。

## 完成状态

```text
UploadsController → 接收 multipart/form-data
IImageStorageService → 定义存储能力
LocalImageStorageService → 验证并写入本地文件
UseStaticFiles → 公开 /uploads URL
Docker Bind Mount → Container 重建后保留文件
```

## 参考资料

- [ASP.NET Core File Uploads](https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads)
- [ASP.NET Core Static Files](https://learn.microsoft.com/aspnet/core/fundamentals/static-files)
- [Docker Storage](https://docs.docker.com/engine/storage/)

## 主线导航

- 上一步：[自动创建分类并按分类筛选文章](./14b-create-and-filter-blog-categories.md)
- 下一步：[为 ASP.NET Core 博客 API 加入 Identity 管理员](./15-add-aspnet-core-identity-admin.md)
