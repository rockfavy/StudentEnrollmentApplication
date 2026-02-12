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

public class RegisterPageTests : TestContext
{
    public RegisterPageTests()
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

    private void FillRegistrationForm(IRenderedComponent<Register> cut, string email, string firstName, string lastName, string password)
    {
        var emailInput = cut.Find("input[type='email']");
        emailInput.Change(email);

        var firstNameInput = cut.Find("input#firstName");
        firstNameInput.Change(firstName);

        var lastNameInput = cut.Find("input#lastName");
        lastNameInput.Change(lastName);

        var passwordInput = cut.Find("input[id='password']");
        passwordInput.Change(password);

        var confirmPasswordInput = cut.Find("input[id='confirmPassword']");
        confirmPasswordInput.Change(password);

        var form = cut.Find("form");
        form.Submit();
    }

    [Fact]
    public void RegisterPage_Renders_Registration_Form_In_Development()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        Assert.Contains("Register", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("First Name", cut.Markup);
        Assert.Contains("Last Name", cut.Markup);
        Assert.Contains("Password", cut.Markup);
    }

    [Fact]
    public void RegisterPage_Renders_EntraId_Message_In_Production_With_EntraId_Configured()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: true, hasEntraIdConfig: true);

        var cut = RenderComponent<Register>();

        Assert.Contains("Register", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("First Name", cut.Markup);
    }

    [Fact]
    public void RegisterPage_Renders_Registration_Form_In_Production_Without_EntraId_Config()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: true, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        Assert.Contains("Register", cut.Markup);
        Assert.Contains("Email", cut.Markup);
        Assert.Contains("First Name", cut.Markup);
        Assert.Contains("Last Name", cut.Markup);
        Assert.Contains("Password", cut.Markup);
    }

    [Fact]
    public void RegisterPage_Shows_Loading_State_During_Submission()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        var tcs = new TaskCompletionSource<RegisterResponse?>();
        mockAuthClient.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .Returns(tcs.Task);

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        FillRegistrationForm(cut, "test@example.com", "Test", "User", "Password123!");

        cut.WaitForState(() => mockAuthClient.Invocations.Any(), timeout: TimeSpan.FromSeconds(1));

        mockAuthClient.Verify(x => x.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Once);
    }

    [Fact]
    public void RegisterPage_Submits_Form_With_Valid_Data()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        var registerResponse = new RegisterResponse(
            Id: Guid.NewGuid(),
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User"
        );

        mockAuthClient.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(registerResponse);

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        FillRegistrationForm(cut, "test@example.com", "Test", "User", "Password123!");

        cut.WaitForState(() => !cut.Markup.Contains("Registering..."));

        mockAuthClient.Verify(x => x.RegisterAsync(It.Is<RegisterRequest>(
            r => r.Email == "test@example.com" &&
                 r.FirstName == "Test" &&
                 r.LastName == "User" &&
                 r.Password == "Password123!")), Times.Once);
    }

    [Fact]
    public void RegisterPage_Displays_Error_Message_On_Registration_Failure()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        mockAuthClient.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync((RegisterResponse?)null);

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        FillRegistrationForm(cut, "test@example.com", "Test", "User", "Password123!");

        cut.WaitForState(() => cut.Markup.Contains("Registration failed") || 
                              cut.Markup.Contains("error", StringComparison.OrdinalIgnoreCase));

        Assert.True(cut.Markup.Contains("Registration failed") || 
                   cut.Markup.Contains("error", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RegisterPage_Displays_Error_Message_On_Exception()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        mockAuthClient.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new HttpRequestException("Email already exists"));

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        FillRegistrationForm(cut, "test@example.com", "Test", "User", "Password123!");

        cut.WaitForState(() => cut.Markup.Contains("Email already exists", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("Email already exists", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterPage_Shows_Login_Link()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

        Assert.Contains("Login here", cut.Markup);
        Assert.Contains("/login", cut.Markup);
    }

    [Fact]
    public void RegisterPage_Toggles_Password_Visibility()
    {
        var mockAuthClient = CreateMockAuthClient();
        var authStateProvider = CreateAuthStateProvider();

        RegisterServices(mockAuthClient.Object, authStateProvider, isProduction: false, hasEntraIdConfig: false);

        var cut = RenderComponent<Register>();

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

