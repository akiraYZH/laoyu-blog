---
title: "使用 WebApplicationFactory 测试博客授权与发布流程"
description: "建立独立测试 Host，替换数据库并通过真实 HTTP Pipeline 验证登录、401、403、CRUD 和发布可见性。"
tags:
  - ASP.NET Core
  - xUnit
  - WebApplicationFactory
  - Integration Testing
---

# 使用 WebApplicationFactory 测试博客授权与发布流程

手动用 Postman 验证一次不能防止以后回归。发布功能跨越 Authentication、Authorization、Controller、Service 和 EF Core，更适合通过 ASP.NET Core 集成测试验证完整请求链。

## 建立测试项目

测试项目需要 xUnit、`Microsoft.AspNetCore.Mvc.Testing` 和测试数据库 Provider，并引用 API 项目：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.10" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.10" />
  <PackageReference Include="xunit" Version="2.9.3" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="../../BlogApi.csproj" />
</ItemGroup>
```

顶层语句生成的 `Program` 默认不方便被测试程序集引用，因此在 `Program.cs` 末尾加入：

```csharp
public partial class Program
{
}
```

## 使用 Factory 创建测试 Host

```csharp
public sealed class BlogApiFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(
                    $"BlogApiTests-{Guid.NewGuid()}"));
        });
    }
}
```

Factory 启动真实 ASP.NET Core Pipeline，但用隔离的测试数据库替换 PostgreSQL。测试不需要占用 8080，也不会修改开发数据库。

测试配置仍需提供假的 Connection String、JWT Issuer、Audience、Key 和管理员账号，因为应用在建立 Host 时会验证这些配置。测试 Secret 只能用于测试，不能复用生产凭据。

## 测试 401 与 403

未带 Token 调用受保护端点：

```csharp
var response = await client.PostAsJsonAsync(
    "/api/blogs",
    validRequest);

Assert.Equal(
    HttpStatusCode.Unauthorized,
    response.StatusCode);
```

再创建一个没有 Admin Role 的有效用户，登录后调用同一端点，应得到 `403 Forbidden`。两条测试分别证明 Authentication 和 Authorization 生效。

## 测试发布可见性

发布测试需要验证完整状态序列：

```text
1. Admin 登录
2. 创建 Draft
3. 清除 Authorization Header
4. 游客 GET Draft → 404
5. Admin 再次登录并 Publish
6. 游客 GET Published → 200
```

取消发布测试执行相反流程，并验证最终 `PublishedAtUtc` 为 null。这样测试的是外部可观察行为，而不是只检查某个方法是否被调用。

## 单元测试与集成测试的边界

`BlogPost.Publish()` 不依赖 HTTP 或数据库，使用快速单元测试：

```csharp
[Fact]
public void Publish_DraftPost_PublishesPost()
{
    var post = new BlogPost();

    var changed = post.Publish();

    Assert.True(changed);
    Assert.Equal(BlogPostStatus.Published, post.Status);
    Assert.NotNull(post.PublishedAtUtc);
}
```

下面这些则由集成测试覆盖：

- JWT 是否能登录；
- `[Authorize]` 是否返回 401 或 403；
- Route 和 Model Binding 是否正确；
- Service 与 Controller 是否组合成预期 HTTP Contract；
- 发布后游客是否真的能读取。

EF Core InMemory Provider 不等于 PostgreSQL，不能证明唯一索引、SQL 语法或数据库类型行为完全一致。涉及 Npgsql 和 Migration 的行为仍应使用 PostgreSQL 测试或独立运行验证。

## 运行

```bash
dotnet test
```

预期所有领域规则、DTO Validation 和 API 集成测试通过。失败时先读测试名称，再读 Expected、Actual 和 Stack Trace，不要只看最后的 `Build failed`。

## 总结

测试项目已经能够启动隔离的 API Host，通过真实 HTTP 请求验证登录、权限、CRUD 和发布生命周期；纯状态规则留给单元测试，数据库专属行为不由 InMemory Provider 代替证明。

## 参考资料

- [ASP.NET Core integration tests](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- [Testing with EF Core](https://learn.microsoft.com/ef/core/testing/)

## 主线导航

- 上一步：[为博客加入草稿、发布与取消发布](./16-add-draft-publish-workflow.md)
- 下一步：[为 API 和 PostgreSQL 添加 Health Check](./18-add-api-database-health-check.md)
