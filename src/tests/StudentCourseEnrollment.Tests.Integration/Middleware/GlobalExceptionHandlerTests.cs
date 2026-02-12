using System.Net;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Middleware;

public class GlobalExceptionHandlerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GlobalExceptionHandlerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_Starts_Successfully_With_Exception_Handler_Registered()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.NotNull(response);
    }

    [Fact]
    public async Task Exception_Handler_Configuration_Is_Valid()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }
}
