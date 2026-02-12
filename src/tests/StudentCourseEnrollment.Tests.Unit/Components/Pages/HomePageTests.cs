using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Components.Pages;
using StudentCourseEnrollment.Frontend.Services;
using StudentCourseEnrollment.Shared;
using System.Security.Claims;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Components.Pages;

public class HomePageTests : TestContext
{
    private CustomAuthenticationStateProvider CreateAuthStateProvider(string? token = null)
    {
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        
        var tokenToReturn = token;
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(
            It.Is<string>(s => s == "localStorage.getItem"), 
            It.Is<object[]>(args => args != null && args.Length > 0 && args[0].ToString() == "authToken")))
            .ReturnsAsync(tokenToReturn);
        
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string method, object[] args) => 
            {
                if (method == "localStorage.getItem" && args != null && args.Length > 0 && args[0]?.ToString() == "authToken")
                {
                    return tokenToReturn;
                }
                return null;
            });
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        return new CustomAuthenticationStateProvider(mockJSRuntime.Object);
    }

    private TestAuthorizationContext RegisterServices(CustomAuthenticationStateProvider authStateProvider, bool syncAuthState = false)
    {
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(authStateProvider);
        var authContext = this.AddTestAuthorization();
        
        if (syncAuthState)
        {
            var authState = authStateProvider.GetAuthenticationStateAsync().Result;
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                authContext.SetAuthorized(authState.User.Identity.Name ?? "TestUser");
                var roleClaim = authState.User.FindFirst(ClaimTypes.Role);
                if (roleClaim != null)
                {
                    authContext.SetRoles(roleClaim.Value);
                }
            }
            else
            {
                authContext.SetNotAuthorized();
            }
        }
        
        return authContext;
    }

    [Fact]
    public void HomePage_Renders_Page_Title()
    {
        var authStateProvider = CreateAuthStateProvider();
        var authContext = RegisterServices(authStateProvider);

        var cut = RenderComponent<Home>();

        Assert.Contains("Student Course Enrollment System", cut.Markup);
    }

 
}
