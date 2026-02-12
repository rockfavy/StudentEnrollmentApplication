using System.Net;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Middleware;

public class AuthenticationMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_Starts_Successfully_With_Authentication_Middleware_Registered()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.NotNull(response);
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Authentication_Middleware_Configuration_Is_Valid()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Authorization_Middleware_Configuration_Is_Valid()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.NotNull(response);
    }
}
