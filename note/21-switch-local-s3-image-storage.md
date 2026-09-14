---
title: "通过配置切换本地与 Amazon S3 图片存储"
description: "复用 IImageStorageService，根据 Storage:Provider 在本地 Volume 和 Amazon S3 实现之间切换。"
tags:
  - ASP.NET Core
  - Amazon S3
  - Dependency Injection
  - Strategy Pattern
---

# 通过配置切换本地与 Amazon S3 图片存储

上一篇的本地上传适合开发机和普通 VPS；EC2 场景中，把图片放入 S3 可以让应用 Container 保持无状态。本文不修改 Upload Controller，而是通过同一个接口选择不同实现。

## 为什么使用配置开关

项目需要同时保留两种部署方式：

```text
Storage:Provider = Local → 本地磁盘 + Docker Volume
Storage:Provider = S3    → S3 Bucket + 临时读取 URL
```

Controller 只依赖 `IImageStorageService`，启动时由依赖注入决定具体实现。

## 扩展存储接口

`Services/ImageStorage/IImageStorageService.cs`：

```csharp
public interface IImageStorageService
{
    Task<ImageUploadResult> SaveAsync(
        IFormFile file,
        CancellationToken cancellationToken);

    Task<string?> CreateReadUrlAsync(string fileName);
}
```

`SaveAsync` 保存文件并返回文章应记录的稳定 URL；`CreateReadUrlAsync` 为某个文件生成实际可读取的位置。

## 定义 S3 配置对象

`Options/S3StorageOptions.cs`：

```csharp
public sealed class S3StorageOptions
{
    public const string SectionName = "S3Storage";

    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = "uploads";
}
```

环境变量使用双下划线表达配置层级：

```text
Storage__Provider=S3
S3Storage__BucketName=example-blog-images
S3Storage__Region=ca-central-1
S3Storage__KeyPrefix=uploads
```

不要把 AWS Access Key 写入 `.env` 或仓库。运行在 EC2 时，让 AWS SDK 通过实例的 IAM Role 获取短期凭据。

## 实现 S3 写入

`S3ImageStorageService` 验证大小和扩展名后生成随机 Object Key：

```csharp
var fileName = $"{Guid.NewGuid():N}{extension}";
var keyPrefix = _options.KeyPrefix.Trim('/');
var objectKey = $"{keyPrefix}/{fileName}";

await using var stream = file.OpenReadStream();

await _s3Client.PutObjectAsync(
    new PutObjectRequest
    {
        BucketName = _options.BucketName,
        Key = objectKey,
        InputStream = stream,
        ContentType = contentType
    },
    cancellationToken);
```

API 返回自己的读取路由，而不是把 Bucket 设置为 Public：

```csharp
return ImageUploadResult.Success(
    $"/api/images/{fileName}");
```

读取时创建短期 Presigned URL：

```csharp
var request = new GetPreSignedUrlRequest
{
    BucketName = _options.BucketName,
    Key = $"{keyPrefix}/{fileName}",
    Expires = DateTime.UtcNow.AddMinutes(5),
    Verb = HttpVerb.GET
};

return await _s3Client.GetPreSignedURLAsync(request);
```

文章中的 URL 保持 `/api/images/<file>` 不变；每次访问时，API 再返回 `302 Redirect` 到五分钟有效的 S3 URL。这样切换 Bucket 或访问策略时无需更新所有 Markdown。

## 添加读取端点

`UploadsController` 的读取端点允许匿名访问：

```csharp
[AllowAnonymous]
[HttpGet("images/{fileName}")]
public async Task<IActionResult> GetImage(string fileName)
{
    var imageUrl =
        await _imageStorageService.CreateReadUrlAsync(fileName);

    if (imageUrl is null)
    {
        return NotFound();
    }

    return Redirect(imageUrl);
}
```

上传仍由 Controller 上的 Admin 授权保护。`fileName == Path.GetFileName(fileName)` 检查用于拒绝目录穿越形式的输入。

## 按配置注册实现

`Program.cs`：

```csharp
var storageProvider = builder.Configuration["Storage:Provider"];
var useS3Storage = string.Equals(
    storageProvider,
    "S3",
    StringComparison.OrdinalIgnoreCase);

if (useS3Storage)
{
    // 绑定并验证 S3StorageOptions，并注册 IAmazonS3。
    builder.Services.AddScoped<
        IImageStorageService,
        S3ImageStorageService>();
}
else
{
    builder.Services.AddScoped<
        IImageStorageService,
        LocalImageStorageService>();
}
```

只有选择 S3 时才要求 Bucket 和 Region 配置完整。因此 VPS 使用 Local 时不需要伪造 AWS 配置。

## IAM 最小权限

应用至少需要对指定前缀执行写入和读取：

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:GetObject"],
      "Resource": "arn:aws:s3:::example-blog-images/uploads/*"
    }
  ]
}
```

Policy 应附加到 EC2 IAM Role，而不是创建长期 Access Key。Bucket 名、Region 和 ARN 要替换为实际资源。

## 验证两种实现

本地模式设置 `Storage__Provider=Local`。上传后应返回 `/uploads/<file>`，重新创建 API Container 后图片仍能读取。

S3 模式设置 `Storage__Provider=S3`。上传后应返回 `/api/images/<file>`；访问该地址应得到 302，随后浏览器加载 S3 对象。还要在 S3 Console 确认 Object 位于配置的 Prefix。

## 当前边界

此实现仍由 API 接收整个文件，再上传 S3，不是浏览器直传。若流量增加，可以增加 Presigned Upload URL，让浏览器直接写 S3；那属于后续优化，不影响当前接口抽象。

## 参考资料

- [AWS SDK for .NET S3 examples](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/csharp_s3_code_examples.html)
- [IAM roles for Amazon EC2](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/iam-roles-for-amazon-ec2.html)
- [S3 presigned URLs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html)

## 主线导航

- 上一步：[使用 Docker Compose、Nginx 和健康检查运行生产栈](./20-production-compose-nginx.md)
- 下一步：[使用 GitHub Actions 验证前后端](./22-github-actions-ci.md)
