using System.Net;

namespace LaoyuBlog.Api.Tests;

public sealed class HealthCheckTests
    : IClassFixture<BlogApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(BlogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_WhenApiAndDatabaseAreHealthy_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
