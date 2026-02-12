using System.Security.Claims;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Services;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Services;

public class CustomAuthenticationStateProviderTests
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
    public async Task GetAuthenticationStateAsync_Should_Return_Current_User()
    {
        var mockJsRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        var token = "test-token";
        var tokenKey = "authToken";
        mockJsRuntime.Setup(x => x.InvokeAsync<string?>(It.Is<string>(s => s == "localStorage.getItem"), It.Is<object[]>(args => args != null && args.Length > 0 && args[0] != null && args[0].ToString() == tokenKey)))
            .ReturnsAsync(token);
        mockJsRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);
        var claims = new[] { new Claim(ClaimTypes.Name, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, "role");
        var principal = new ClaimsPrincipal(identity);

        await provider.SetAuthenticationStateAsync(principal, token);

        var authState = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(authState);
        Assert.True(authState.User.Identity?.IsAuthenticated ?? false);
        Assert.Equal("test@example.com", authState.User.Identity?.Name);
    }

    [Fact]
    public async Task GetAccessToken_Should_Return_Stored_Token()
    {
        var mockJsRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        var token = "test-access-token-123";
        var tokenKey = "authToken";
        mockJsRuntime.Setup(x => x.InvokeAsync<string?>(It.Is<string>(s => s == "localStorage.getItem"), It.Is<object[]>(args => args != null && args.Length > 0 && args[0] != null && args[0].ToString() == tokenKey)))
            .ReturnsAsync(token);
        mockJsRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);
        var claims = new[] { new Claim(ClaimTypes.Name, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, "role");
        var principal = new ClaimsPrincipal(identity);

        await provider.SetAuthenticationStateAsync(principal, token);

        var retrievedToken = provider.GetAccessToken();

        Assert.Equal(token, retrievedToken);
    }

    [Fact]
    public async Task GetAccessToken_When_No_Token_Set_Should_Return_Null()
    {
        var mockJsRuntime = CreateMockJSRuntime();
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);

        await provider.GetAuthenticationStateAsync();

        var token = provider.GetAccessToken();

        Assert.Null(token);
    }

    [Fact]
    public async Task ClearAuthenticationState_Should_Reset_User_And_Token()
    {
        var mockJsRuntime = CreateMockJSRuntime();
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);
        var claims = new[] { new Claim(ClaimTypes.Name, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, "role");
        var principal = new ClaimsPrincipal(identity);

        await provider.SetAuthenticationStateAsync(principal, "test-token");
        await provider.ClearAuthenticationStateAsync();

        var authState = await provider.GetAuthenticationStateAsync();
        var token = provider.GetAccessToken();

        Assert.False(authState.User.Identity?.IsAuthenticated ?? false);
        Assert.Null(token);
    }

    [Fact]
    public async Task SetAuthenticationState_Should_Trigger_AuthenticationStateChanged()
    {
        var mockJsRuntime = CreateMockJSRuntime();
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object);
        var claims = new[] { new Claim(ClaimTypes.Name, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, "role");
        var principal = new ClaimsPrincipal(identity);
        var eventTriggered = false;

        provider.AuthenticationStateChanged += (task) => { eventTriggered = true; };

        await provider.SetAuthenticationStateAsync(principal, "test-token");

        await Task.Delay(100);

        Assert.True(eventTriggered);
    }
}
