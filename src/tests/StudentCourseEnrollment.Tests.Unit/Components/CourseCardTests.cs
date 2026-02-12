using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Frontend.Components.Shared;
using StudentCourseEnrollment.Frontend.Services;
using StudentCourseEnrollment.Shared.DTOs.Courses;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Components;

public class CourseCardTests : TestContext
{
    private Mock<IEnrollmentsClient> CreateMockEnrollmentsClient()
    {
        return new Mock<IEnrollmentsClient>();
    }

    private CustomAuthenticationStateProvider CreateAuthStateProvider(bool isAuthenticated = true)
    {
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        var tokenKey = "authToken";
        var token = "test-token";
        
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.Is<string>(s => s == "localStorage.getItem"), It.Is<object[]>(args => args != null && args.Length > 0 && args[0] != null && args[0].ToString() == tokenKey)))
            .ReturnsAsync(isAuthenticated ? token : (string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);

        var provider = new CustomAuthenticationStateProvider(mockJSRuntime.Object);

        if (isAuthenticated)
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@example.com"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            provider.SetAuthenticationStateAsync(principal, token).GetAwaiter().GetResult();
        }

        return provider;
    }

    private void RegisterServices(IEnrollmentsClient enrollmentsClient, CustomAuthenticationStateProvider authStateProvider)
    {
        Services.AddSingleton(enrollmentsClient);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(authStateProvider);
        
        var authContext = this.AddTestAuthorization();
        var authState = authStateProvider.GetAuthenticationStateAsync().Result;
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            authContext.SetAuthorized(authState.User.Identity.Name ?? "TestUser");
        }
        else
        {
            authContext.SetNotAuthorized();
        }
    }

    private CourseDto CreateTestCourse()
    {
        return new CourseDto(
            Id: Guid.NewGuid(),
            Name: "Introduction to Programming",
            Description: "Learn the fundamentals of programming",
            Capacity: 30,
            CurrentEnrollments: 15,
            CreatedAt: DateTime.UtcNow.AddMonths(-2)
        );
    }

    [Fact]
    public void CourseCard_Renders_Course_Information()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var course = CreateTestCourse();

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        Assert.Contains(course.Name, cut.Markup);
        Assert.Contains(course.Description, cut.Markup);
        Assert.Contains(course.Capacity.ToString(), cut.Markup);
    }

    [Fact]
    public void CourseCard_Displays_Created_Date()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var course = CreateTestCourse();
        var expectedDate = course.CreatedAt.ToString("MMM dd, yyyy");

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        Assert.Contains(expectedDate, cut.Markup);
    }

    [Fact]
    public void CourseCard_Shows_Enroll_Button_When_Authenticated()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider(isAuthenticated: true);
        var course = CreateTestCourse();

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        cut.WaitForAssertion(() => Assert.Contains("Enroll", cut.Markup), timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CourseCard_Shows_Login_Link_When_Not_Authenticated()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider(isAuthenticated: false);
        var course = CreateTestCourse();

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Login to Enroll", cut.Markup);
            Assert.Contains("/login", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CourseCard_Enrolls_Successfully()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var course = CreateTestCourse();
        var enrollmentResponse = new EnrollResponse(Guid.NewGuid(), Guid.NewGuid(), course.Id, DateTime.UtcNow);

        mockEnrollmentsClient.Setup(x => x.EnrollAsync(course.Id))
            .ReturnsAsync(enrollmentResponse);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        cut.WaitForAssertion(() => Assert.Contains("Enroll", cut.Markup), timeout: TimeSpan.FromSeconds(2));

        var enrollButton = cut.Find("button.btn-primary");
        enrollButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("Successfully enrolled", cut.Markup), timeout: TimeSpan.FromSeconds(5));

        mockEnrollmentsClient.Verify(x => x.EnrollAsync(course.Id), Times.Once);
    }

    [Fact]
    public void CourseCard_Shows_Error_On_Enrollment_Failure()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var course = CreateTestCourse();

        mockEnrollmentsClient.Setup(x => x.EnrollAsync(course.Id))
            .ThrowsAsync(new HttpRequestException("Enrollment failed"));

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        cut.WaitForAssertion(() => Assert.Contains("Enroll", cut.Markup), timeout: TimeSpan.FromSeconds(2));

        var enrollButton = cut.Find("button.btn-primary");
        enrollButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Markup.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                       cut.Markup.Contains("Enrollment failed"));
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CourseCard_Disables_Button_During_Enrollment()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var course = CreateTestCourse();
        var tcs = new TaskCompletionSource<EnrollResponse?>();

        mockEnrollmentsClient.Setup(x => x.EnrollAsync(course.Id))
            .Returns(tcs.Task);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CourseCard>(parameters => parameters
                .Add(p => p.Course, course)));

        cut.WaitForAssertion(() => Assert.Contains("Enroll", cut.Markup), timeout: TimeSpan.FromSeconds(2));

        var enrollButton = cut.Find("button.btn-primary");
        enrollButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Enrolling...", cut.Markup);
            Assert.True(enrollButton.HasAttribute("disabled"));
        }, timeout: TimeSpan.FromSeconds(2));

        tcs.SetResult(new EnrollResponse(Guid.NewGuid(), Guid.NewGuid(), course.Id, DateTime.UtcNow));
    }
}

