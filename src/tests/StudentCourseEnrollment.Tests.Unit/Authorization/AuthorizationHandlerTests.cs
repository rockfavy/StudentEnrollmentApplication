using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Authorization;
using StudentCourseEnrollment.Frontend.Services;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Authorization;

public class AuthorizationHandlerTests
{
    private Mock<IJSRuntime> CreateMockJSRuntime()
    {
        var mock = new Mock<IJSRuntime>(MockBehavior.Loose);
        mock.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mock.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        return mock;
    }

    [Fact]
    public async Task SendAsync_With_Token_And_No_Authorization_Header_Should_Add_Bearer_Token()
    {
        var token = "test-token-123";
        var mockJsRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        
        var tokenKey = "authToken";
        mockJsRuntime.Setup(x => x.InvokeAsync<string?>(It.Is<string>(s => s == "localStorage.getItem"), It.Is<object[]>(args => args != null && args.Length > 0 && args[0] != null && args[0].ToString() == tokenKey)))
            .ReturnsAsync(token);
        mockJsRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);
        
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        
        await provider.SetAuthenticationStateAsync(principal, token);

        var retrievedToken = await provider.GetAccessTokenAsync();
        Assert.Equal(token, retrievedToken);

        var handler = new AuthorizationHandler(provider, new TestNavigationManager());
        var innerHandler = new TestHttpMessageHandler();
        handler.InnerHandler = innerHandler;

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        await httpClient.SendAsync(request);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.True(innerHandler.LastRequest!.Headers.Contains("Authorization"));
        var authHeader = innerHandler.LastRequest.Headers.Authorization;
        Assert.NotNull(authHeader);
        Assert.Equal("Bearer", authHeader!.Scheme);
        Assert.Equal(token, authHeader.Parameter);
    }

    [Fact]
    public async Task SendAsync_With_No_Token_Should_Not_Add_Authorization_Header()
    {
        var mockJsRuntime = CreateMockJSRuntime();
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);

        var handler = new AuthorizationHandler(provider, new TestNavigationManager());
        var innerHandler = new TestHttpMessageHandler();
        handler.InnerHandler = innerHandler;

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        await httpClient.SendAsync(request);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.False(innerHandler.LastRequest!.Headers.Contains("Authorization"));
    }

    [Fact]
    public async Task SendAsync_With_Existing_Authorization_Header_Should_Not_Override()
    {
        var token = "test-token-123";
        var existingToken = "existing-token";
        var mockJsRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        
        var tokenKey = "authToken";
        mockJsRuntime.Setup(x => x.InvokeAsync<string?>(It.Is<string>(s => s == "localStorage.getItem"), It.Is<object[]>(args => args != null && args.Length > 0 && args[0] != null && args[0].ToString() == tokenKey)))
            .ReturnsAsync(token);
        mockJsRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);
        await provider.SetAuthenticationStateAsync(
            new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "test") },
                    "jwt",
                    System.Security.Claims.ClaimTypes.Name,
                    "role")),
            token);

        var handler = new AuthorizationHandler(provider, new TestNavigationManager());
        var innerHandler = new TestHttpMessageHandler();
        handler.InnerHandler = innerHandler;

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", existingToken);

        await httpClient.SendAsync(request);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.True(innerHandler.LastRequest!.Headers.Contains("Authorization"));
        var authHeader = innerHandler.LastRequest.Headers.Authorization;
        Assert.Equal(existingToken, authHeader!.Parameter);
    }

    [Fact]
    public async Task SendAsync_With_Non_Custom_Provider_Should_Not_Add_Authorization_Header()
    {
        var mockProvider = new Mock<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>();
        var mockJsRuntime = CreateMockJSRuntime();

        var handler = new AuthorizationHandler(mockProvider.Object, new TestNavigationManager());
        var innerHandler = new TestHttpMessageHandler();
        handler.InnerHandler = innerHandler;

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        await httpClient.SendAsync(request);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.False(innerHandler.LastRequest!.Headers.Contains("Authorization"));
    }
}

internal class TestHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}

internal class TestNavigationManager : NavigationManager
{
    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/test");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Uri = ToAbsoluteUri(uri).ToString();
    }
}
