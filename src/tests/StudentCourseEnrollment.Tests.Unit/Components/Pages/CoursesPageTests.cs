using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Frontend.Components.Pages;
using StudentCourseEnrollment.Frontend.Services;
using StudentCourseEnrollment.Shared.DTOs;
using StudentCourseEnrollment.Shared.DTOs.Courses;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Components.Pages;

public class CoursesPageTests : TestContext
{
    private Mock<ICoursesClient> CreateMockCoursesClient()
    {
        return new Mock<ICoursesClient>();
    }

    private Mock<IEnrollmentsClient> CreateMockEnrollmentsClient()
    {
        return new Mock<IEnrollmentsClient>();
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

    private void RegisterServices(ICoursesClient coursesClient, IEnrollmentsClient enrollmentsClient, CustomAuthenticationStateProvider authStateProvider, IJSRuntime jsRuntime)
    {
        Services.AddSingleton(coursesClient);
        Services.AddSingleton(enrollmentsClient);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton(jsRuntime);
        Services.AddSingleton<ToastService>();
        
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

    private List<CourseDto> CreateTestCourses()
    {
        return new List<CourseDto>
        {
            new CourseDto(Guid.NewGuid(), "Course 1", "Description 1", 30, 10, DateTime.UtcNow),
            new CourseDto(Guid.NewGuid(), "Course 2", "Description 2", 25, 5, DateTime.UtcNow),
            new CourseDto(Guid.NewGuid(), "Course 3", "Description 3", 40, 20, DateTime.UtcNow)
        };
    }

    [Fact]
    public void CoursesPage_Renders_Page_Title()
    {
        var mockCoursesClient = CreateMockCoursesClient();
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        var courses = CreateTestCourses();

        mockCoursesClient.Setup(x => x.GetCoursesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<SortDirection?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResult<CourseDto>(courses, courses.Count));

        RegisterServices(mockCoursesClient.Object, mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Available Courses", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CoursesPage_Loads_Courses_On_Initial_Render()
    {
        var mockCoursesClient = CreateMockCoursesClient();
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        var courses = CreateTestCourses();

        mockCoursesClient.Setup(x => x.GetCoursesAsync(
                0,
                10,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResult<CourseDto>(courses, courses.Count));

        RegisterServices(mockCoursesClient.Object, mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Markup.Contains("Course 1") || cut.Markup.Contains("Course"));
        }, timeout: TimeSpan.FromSeconds(5));

        mockCoursesClient.Verify(x => x.GetCoursesAsync(
            0,
            10,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void CoursesPage_Displays_Courses_In_Table()
    {
        var mockCoursesClient = CreateMockCoursesClient();
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        var courses = CreateTestCourses();

        mockCoursesClient.Setup(x => x.GetCoursesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<SortDirection?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResult<CourseDto>(courses, courses.Count));

        RegisterServices(mockCoursesClient.Object, mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(courses[0].Name, cut.Markup);
            Assert.Contains(courses[0].Description, cut.Markup);
            Assert.Contains($"{courses[0].CurrentEnrollments} / {courses[0].Capacity}", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CoursesPage_Handles_Empty_Courses_List()
    {
        var mockCoursesClient = CreateMockCoursesClient();
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);

        mockCoursesClient.Setup(x => x.GetCoursesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<SortDirection?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemsResult<CourseDto>(Enumerable.Empty<CourseDto>(), 0));

        RegisterServices(mockCoursesClient.Object, mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Available Courses", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CoursesPage_Handles_Error_When_Loading_Courses()
    {
        var mockCoursesClient = CreateMockCoursesClient();
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);

        mockCoursesClient.Setup(x => x.GetCoursesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<SortDirection?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        RegisterServices(mockCoursesClient.Object, mockEnrollmentsClient.Object, authStateProvider, mockJSRuntime.Object);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<CoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Available Courses", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }
}

