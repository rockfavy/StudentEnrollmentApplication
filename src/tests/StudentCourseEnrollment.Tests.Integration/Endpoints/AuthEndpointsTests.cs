using System.Net;
using System.Net.Http.Json;
using StudentCourseEnrollment.Shared.DTOs.Auth;
using StudentCourseEnrollment.Tests.Integration.TestHelpers;
using Xunit;

namespace StudentCourseEnrollment.Tests.Integration.Endpoints;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_With_Valid_Request_Should_Return_Success()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest(
            Email: $"test{Guid.NewGuid()}@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "Password123!"
        );

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
    }

    [Fact]
    public async Task Register_With_Duplicate_Email_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();
        var email = $"duplicate{Guid.NewGuid()}@example.com";
        var request1 = new RegisterRequest(
            Email: email,
            FirstName: "First",
            LastName: "User",
            Password: "Password123!"
        );
        var request2 = new RegisterRequest(
            Email: email,
            FirstName: "Second",
            LastName: "User",
            Password: "Password123!"
        );

        var response1 = await client.PostAsJsonAsync("api/auth/register", request1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var response2 = await client.PostAsJsonAsync("api/auth/register", request2);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task Register_With_Invalid_Email_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest(
            Email: "invalid-email",
            FirstName: "Test",
            LastName: "User",
            Password: "Password123!"
        );

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Should_Return_Token()
    {
        var client = _factory.CreateClient();
        var email = $"login{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        
        var registerRequest = new RegisterRequest(
            Email: email,
            FirstName: "Login",
            LastName: "User",
            Password: password
        );
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            Email: email,
            Password: password
        );

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequest(
            Email: "nonexistent@example.com",
            Password: "WrongPassword"
        );

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"wrongpass{Guid.NewGuid()}@example.com";
        
        var registerRequest = new RegisterRequest(
            Email: email,
            FirstName: "Test",
            LastName: "User",
            Password: "CorrectPassword123!"
        );
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(
            Email: email,
            Password: "WrongPassword123!"
        );

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
