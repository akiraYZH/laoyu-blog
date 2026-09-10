using System.Net;
using System.Net.Http.Json;
using laoyu_blog_backend.Dtos.Auth;
using System.Net.Http.Headers;
using laoyu_blog_backend.Dtos;

namespace LaoyuBlog.Api.Tests;

public sealed class BlogAuthorizationTests
    : IClassFixture<BlogApiFactory>
{
    private readonly HttpClient _client;
    private readonly BlogApiFactory _factory;

    public BlogAuthorizationTests(
        BlogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPosts_WithoutToken_ReturnsOk()
    {
        var response = await _client.GetAsync(
            "/api/blogs?page=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithoutToken_ReturnsUnauthorized()
    {
        var request = new
        {
            title = "Integration Test Post",
            slug = "integration-test-post",
            content = "Test content",
            categoryNames = Array.Empty<string>()
        };

        var response = await _client.PostAsJsonAsync(
            "/api/blogs",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsToken()
    {
        // Arrange：准备登录资料
        var request = new
        {
            email = "admin@test.local",
            password = "TestAdmin123!"
        };

        // Act：调用登录 API
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponseDto>();

        // Assert：检查结果
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(result);

        Assert.Equal(
            "Bearer",
            result.TokenType);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));
    }

    [Fact]
    public async Task CreatePost_WithAdminToken_ReturnsCreated()
    {
        // 1. 登录
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "admin@test.local",
                password = "TestAdmin123!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(loginResult);

        // 2. 把 JWT 放进之后请求的 Authorization Header
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                loginResult.TokenType,
                loginResult.AccessToken);

        // 3. 创建文章
        var request = new
        {
            title = "Authorized Integration Test",
            slug = "authorized-integration-test",
            content = "Created by an authenticated administrator.",
            categoryNames = new[] { "Testing" }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/blogs",
            request);

        // 4. 验证 HTTP 响应
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var createdPost = await response.Content
            .ReadFromJsonAsync<BlogPostResponseDto>();

        // 5. 验证返回的文章
        Assert.NotNull(createdPost);
        Assert.True(createdPost.Id > 0);
        Assert.Equal(request.title, createdPost.Title);
        Assert.Equal(request.slug, createdPost.Slug);
    }


    [Fact]
    public async Task UpdatePost_WithAdminToken_ReturnsUpdatedPost()
    {
        await AuthenticateAsAdminAsync();

        var createdPost = await CreateTestPostAsync(
            "Post Before Update",
            "update-integration-test",
            "Content before update.");

        var updateRequest = new
        {
            title = "Post After Update",
            slug = "updated-integration-test",
            content = "Content after update.",
            categoryNames = new[] { "Testing", "Updated" }
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/blogs/{createdPost.Id}",
            updateRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updatedPost = await response.Content
            .ReadFromJsonAsync<BlogPostResponseDto>();

        Assert.NotNull(updatedPost);
        Assert.Equal(createdPost.Id, updatedPost.Id);
        Assert.Equal(updateRequest.title, updatedPost.Title);
        Assert.Equal(updateRequest.slug, updatedPost.Slug);
        Assert.Equal(updateRequest.content, updatedPost.Content);
        Assert.Equal(2, updatedPost.Categories.Count);
    }

    [Fact]
    public async Task DeletePost_WithAdminToken_RemovesPost()
    {
        await AuthenticateAsAdminAsync();

        var createdPost = await CreateTestPostAsync(
            "Post To Delete",
            "delete-integration-test",
            "This post will be deleted.");

        var deleteResponse = await _client.DeleteAsync(
            $"/api/blogs/{createdPost.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/blogs/{createdPost.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }


    [Fact]
    public async Task PublishPost_WithAdminToken_MakesDraftPubliclyReadable()
    {
        await AuthenticateAsAdminAsync();

        var createdPost = await CreateTestPostAsync(
            "Draft Publication Test",
            "draft-publication-test",
            "This draft becomes public after publishing.");

        Assert.Equal("Draft", createdPost.Status);
        Assert.Null(createdPost.PublishedAtUtc);

        _client.DefaultRequestHeaders.Authorization = null;

        var draftResponse = await _client.GetAsync(
            $"/api/blogs/{createdPost.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            draftResponse.StatusCode);

        await AuthenticateAsAdminAsync();

        var publishResponse = await _client.PostAsync(
            $"/api/blogs/{createdPost.Id}/publish",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            publishResponse.StatusCode);

        var publishedPost = await publishResponse.Content
            .ReadFromJsonAsync<BlogPostResponseDto>();

        Assert.NotNull(publishedPost);
        Assert.Equal("Published", publishedPost.Status);
        Assert.NotNull(publishedPost.PublishedAtUtc);

        _client.DefaultRequestHeaders.Authorization = null;

        var publicResponse = await _client.GetAsync(
            $"/api/blogs/{createdPost.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            publicResponse.StatusCode);
    }

    [Fact]
    public async Task UnpublishPost_WithAdminToken_MakesPublishedPostPrivate()
    {
        await AuthenticateAsAdminAsync();

        var createdPost = await CreateTestPostAsync(
            "Unpublish Integration Test",
            "unpublish-integration-test",
            "This published post becomes private again.");

        var publishResponse = await _client.PostAsync(
            $"/api/blogs/{createdPost.Id}/publish",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            publishResponse.StatusCode);

        var unpublishResponse = await _client.PostAsync(
            $"/api/blogs/{createdPost.Id}/unpublish",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            unpublishResponse.StatusCode);

        var unpublishedPost = await unpublishResponse.Content
            .ReadFromJsonAsync<BlogPostResponseDto>();

        Assert.NotNull(unpublishedPost);
        Assert.Equal("Draft", unpublishedPost.Status);
        Assert.Null(unpublishedPost.PublishedAtUtc);

        _client.DefaultRequestHeaders.Authorization = null;

        var publicResponse = await _client.GetAsync(
            $"/api/blogs/{createdPost.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            publicResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithNonAdminToken_ReturnsForbidden()
    {
        const string email = "user@test.local";
        const string password = "TestUser123!";

        await _factory.CreateUserWithoutRoleAsync(
            email,
            password);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult = await loginResponse.Content
            .ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(loginResult);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                loginResult.TokenType,
                loginResult.AccessToken);

        var response = await _client.PostAsJsonAsync(
            "/api/blogs",
            new
            {
                title = "Forbidden Integration Test",
                slug = "forbidden-integration-test",
                content = "A non-admin must not create this post.",
                categoryNames = Array.Empty<string>()
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "admin@test.local",
                password = "TestAdmin123!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(result);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                result.TokenType,
                result.AccessToken);
    }

    private async Task<BlogPostResponseDto> CreateTestPostAsync(
        string title,
        string slug,
        string content)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/blogs",
            new
            {
                title,
                slug,
                content,
                categoryNames = new[] { "Testing" }
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var createdPost = await response.Content
            .ReadFromJsonAsync<BlogPostResponseDto>();

        Assert.NotNull(createdPost);

        return createdPost;
    }
}
