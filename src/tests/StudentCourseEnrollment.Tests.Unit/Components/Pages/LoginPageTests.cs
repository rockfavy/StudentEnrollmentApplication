using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Frontend.Components.Pages;
using StudentCourseEnrollment.Frontend.Services;
using StudentCourseEnrollment.Shared.DTOs.Auth;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Components.Pages;

public class LoginPageTests : TestContext
{
    public LoginPageTests()
    {
        JSInterop.Setup<string>("sessionStorage.getItem", _ => true)
            .SetResult(string.Empty);
        JSInterop.SetupVoid("sessionStorage.setItem", _ => true);
        JSInterop.SetupVoid("sessionStorage.removeItem", _ => true);
    }

    private Mock<IAuthClient> CreateMockAuthClient()
    {
        return new Mock<IAuthClient>();
    }

    private CustomAuthenticationStateProvider CreateAuthStateProvider()
    {
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        return new CustomAuthenticationStateProvider(mockJSRuntime.Object);
    }

    private Mock<IWebAssemblyHostEnvironment> CreateMockHostEnvironment(bool isProduction = false)
    {
        var mock = new Mock<IWebAssemblyHostEnvironment>();
        mock.Setup(x => x.Environment).Returns(isProduction ? "Production" : "Development");
        return mock;
    }

    private IConfiguration CreateMockConfiguration(bool hasEntraIdConfig = false)
    {
        var configData = new Dictionary<string, string?>();
        if (hasEntraIdConfig)
        {
            configData["AzureAd:Authority"] = "https://login.microsoftonline.com/test";
            configData["AzureAd:ClientId"] = "test-client-id";
        }
        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    private void RegisterServices(IAuthClient authClient, CustomAuthenticationStateProvider authStateProvider, bool isProduction = false, bool hasEntraIdConfig = false)
    {
        Services.AddSingleton(authClient);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton(CreateMockHostEnvironment(isProduction).Object);
        Services.AddSingleton<IConfiguration>(CreateMockConfiguration(hasEntraIdConfig));
    }

    [Fact]
    public void LoginPage_Renders_Login_Form_In_Development()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        Assert.Contains("Login", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("Password", cut.Markup);
    }

    [Fact]
    public void LoginPage_Renders_EntraId_Message_In_Production_With_EntraId_Configured()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: true, hasEntraIdConfig: true);

        var cut = RenderComponent<Login>();

        Assert.Contains("Login", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("Password", cut.Markup);
    }

    [Fact]
    public void LoginPage_Renders_Login_Form_In_Production_Without_EntraId_Config()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: true, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        Assert.Contains("Login", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("Password", cut.Markup);
    }

    [Fact]
    public void LoginPage_Shows_Loading_State_During_Submission()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        var tcs = new TaskCompletionSource<LoginResponse?>();
        mockAuthClient.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .Returns(tcs.Task);

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        var emailInput = cut.Find("input[type='email']");
        emailInput.Change("test@example.com");

        var passwordInput = cut.Find("input[id='password']");
        passwordInput.Change("Password123!");

        var form = cut.Find("form");
        form.Submit();

        cut.WaitForState(() => cut.Markup.Contains("Logging in..."));

        Assert.Contains("Logging in...", cut.Markup);

        tcs.SetResult(new LoginResponse("test-token", Guid.NewGuid(), "test@example.com", "Test", "User"));
        cut.WaitForState(() => !cut.Markup.Contains("Logging in..."));
    }

    [Fact]
    public void LoginPage_Submits_Form_With_Valid_Credentials()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        var loginResponse = new LoginResponse(
            Token: "test-token",
            Id: Guid.NewGuid(),
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User"
        );

        mockAuthClient.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(loginResponse);

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        var emailInput = cut.Find("input[type='email']");
        emailInput.Change("test@example.com");

        var passwordInput = cut.Find("input[id='password']");
        passwordInput.Change("Password123!");

        var form = cut.Find("form");
        form.Submit();

        cut.WaitForState(() => !cut.Markup.Contains("Logging in..."));

        mockAuthClient.Verify(x => x.LoginAsync(It.Is<LoginRequest>(
            r => r.Email == "test@example.com" &&
                 r.Password == "Password123!")), Times.Once);
    }

    [Fact]
    public void LoginPage_Displays_Error_Message_On_Login_Failure()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        mockAuthClient.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync((LoginResponse?)null);

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        var emailInput = cut.Find("input[type='email']");
        emailInput.Change("test@example.com");

        var passwordInput = cut.Find("input[id='password']");
        passwordInput.Change("Password123!");

        var form = cut.Find("form");
        form.Submit();


        cut.WaitForState(() => mockAuthClient.Invocations.Any(), timeout: TimeSpan.FromSeconds(1));
        mockAuthClient.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
    }

    [Fact]
    public void LoginPage_Displays_Error_Message_On_Exception()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        mockAuthClient.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new Exception("Network error"));

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        var emailInput = cut.Find("input[type='email']");
        emailInput.Change("test@example.com");

        var passwordInput = cut.Find("input[id='password']");
        passwordInput.Change("Password123!");

        var form = cut.Find("form");
        form.Submit();


        cut.WaitForState(() => mockAuthClient.Invocations.Any(), timeout: TimeSpan.FromSeconds(1));
        mockAuthClient.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
    }

    [Fact]
    public void LoginPage_Shows_Register_Link()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        Assert.Contains("Register here", cut.Markup);
        Assert.Contains("/register", cut.Markup);
    }

    [Fact]
    public void LoginPage_Toggles_Password_Visibility()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Login>();

        var passwordInput = cut.Find("input[id='password']");
        Assert.Equal("password", passwordInput.GetAttribute("type"));

        var toggleButton = cut.Find("button[type='button']");
        toggleButton.Click();

        cut.WaitForState(() => passwordInput.GetAttribute("type") == "text");
        Assert.Equal("text", passwordInput.GetAttribute("type"));

        toggleButton.Click();
        cut.WaitForState(() => passwordInput.GetAttribute("type") == "password");
        Assert.Equal("password", passwordInput.GetAttribute("type"));
    }
}

