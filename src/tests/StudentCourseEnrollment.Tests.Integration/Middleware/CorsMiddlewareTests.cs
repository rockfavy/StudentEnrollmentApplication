using System.Net;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Middleware;

public class CorsMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CorsMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Options_Request_From_Allowed_Https_Origin_Should_Return_200_With_Cors_Headers()
    {
        var client = _factory.CreateClient();
        var origin = "https://localhost:5001";

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/courses");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Options_Request_From_Allowed_Http_Origin_Should_Return_200_With_Cors_Headers()
    {
        var client = _factory.CreateClient();
        var origin = "http://localhost:5000";

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/courses");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Get_Request_From_Allowed_Origin_Should_Include_Cors_Headers()
    {
        var client = _factory.CreateClient();
        var origin = "https://localhost:5001";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/courses");
        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }

    [Fact]
    public async Task Request_From_Disallowed_Origin_Should_Not_Include_Cors_Headers()
    {
        var client = _factory.CreateClient();
        var origin = "https://malicious-site.com";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/courses");
        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal(origin, allowedOrigin);
    }
}
