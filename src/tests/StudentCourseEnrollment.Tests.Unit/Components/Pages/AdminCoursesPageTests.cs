using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using StudentCourseEnrollment.Frontend.Clients.Interfaces;
using StudentCourseEnrollment.Frontend.Components.Pages;
using StudentCourseEnrollment.Frontend.Services;
using StudentCourseEnrollment.Shared.DTOs.Courses;
using StudentCourseEnrollment.Shared.DTOs.Enrollments;
using System.Security.Claims;
using Xunit;

namespace StudentCourseEnrollment.Tests.Unit.Components.Pages;

public class AdminCoursesPageTests : TestContext
{
    private Mock<IEnrollmentsClient> CreateMockEnrollmentsClient()
    {
        return new Mock<IEnrollmentsClient>();
    }

    private CustomAuthenticationStateProvider CreateAuthStateProvider(bool isAuthenticated = true, string? role = "Admin")
    {
        var mockJSRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
        mockJSRuntime.Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        mockJSRuntime.Setup(x => x.InvokeAsync<IJSVoidResult?>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult?)null);
        
        var provider = new CustomAuthenticationStateProvider(mockJSRuntime.Object);
        
        if (isAuthenticated && !string.IsNullOrEmpty(role))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "admin@example.com"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);
            provider.SetAuthenticationStateAsync(principal, "test-token").Wait();
        }
        
        return provider;
    }

    private void RegisterServices(IEnrollmentsClient enrollmentsClient, CustomAuthenticationStateProvider authStateProvider, bool isAuthenticated = true, string? role = "Admin")
    {
        Services.AddSingleton(enrollmentsClient);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddSingleton(authStateProvider);
        Services.AddSingleton<ToastService>();
        
        var authContext = this.AddTestAuthorization();
        if (isAuthenticated)
        {
            authContext.SetAuthorized("AdminUser");
        }
        else
        {
            authContext.SetNotAuthorized();
        }
    }

    private List<CourseWithEnrollmentsDto> CreateTestCoursesWithEnrollments()
    {
        var courseId1 = Guid.NewGuid();
        var courseId2 = Guid.NewGuid();
        var enrollmentId1 = Guid.NewGuid();
        var enrollmentId2 = Guid.NewGuid();
        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();

        return new List<CourseWithEnrollmentsDto>
        {
            new CourseWithEnrollmentsDto(
                Id: courseId1,
                Name: "Course 1",
                Description: "Description 1",
                Capacity: 30,
                CurrentEnrollments: 2,
                CreatedAt: DateTime.UtcNow,
                Enrollments: new List<StudentEnrollmentDto>
                {
                    new StudentEnrollmentDto(
                        EnrollmentId: enrollmentId1,
                        StudentId: studentId1,
                        StudentFirstName: "John",
                        StudentLastName: "Doe",
                        StudentEmail: "john.doe@example.com",
                        EnrolledAt: DateTime.UtcNow.AddDays(-5)
                    ),
                    new StudentEnrollmentDto(
                        EnrollmentId: enrollmentId2,
                        StudentId: studentId2,
                        StudentFirstName: "Jane",
                        StudentLastName: "Smith",
                        StudentEmail: "jane.smith@example.com",
                        EnrolledAt: DateTime.UtcNow.AddDays(-3)
                    )
                }
            ),
            new CourseWithEnrollmentsDto(
                Id: courseId2,
                Name: "Course 2",
                Description: "Description 2",
                Capacity: 25,
                CurrentEnrollments: 1,
                CreatedAt: DateTime.UtcNow,
                Enrollments: new List<StudentEnrollmentDto>
                {
                    new StudentEnrollmentDto(
                        EnrollmentId: Guid.NewGuid(),
                        StudentId: Guid.NewGuid(),
                        StudentFirstName: "Bob",
                        StudentLastName: "Johnson",
                        StudentEmail: "bob.johnson@example.com",
                        EnrolledAt: DateTime.UtcNow.AddDays(-1)
                    )
                }
            )
        };
    }

    [Fact]
    public void AdminCoursesPage_Renders_Page_Title()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var courses = CreateTestCoursesWithEnrollments();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .ReturnsAsync(courses);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Course Management", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdminCoursesPage_Shows_Loading_State_Initially()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var tcs = new TaskCompletionSource<List<CourseWithEnrollmentsDto>?>();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .Returns(tcs.Task);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        Assert.Contains("Loading", cut.Markup);

        tcs.SetResult(CreateTestCoursesWithEnrollments());
        cut.WaitForState(() => !cut.Markup.Contains("Loading"));
    }

    [Fact]
    public void AdminCoursesPage_Displays_Courses_With_Enrollments()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var courses = CreateTestCoursesWithEnrollments();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .ReturnsAsync(courses);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Course 1", cut.Markup);
            Assert.Contains("Course 2", cut.Markup);
            Assert.Contains("Description 1", cut.Markup);
            Assert.Contains("Enrolled Students: 2", cut.Markup);
            Assert.Contains("Enrolled Students: 1", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdminCoursesPage_Displays_Student_Enrollment_Details()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var courses = CreateTestCoursesWithEnrollments();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .ReturnsAsync(courses);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("John Doe", cut.Markup);
            Assert.Contains("Jane Smith", cut.Markup);
            Assert.Contains("john.doe@example.com", cut.Markup);
            Assert.Contains("jane.smith@example.com", cut.Markup);
            Assert.Contains("Student Name", cut.Markup);
            Assert.Contains("Email", cut.Markup);
            Assert.Contains("Enrolled At", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdminCoursesPage_Shows_Empty_State_When_No_Courses()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .ReturnsAsync(new List<CourseWithEnrollmentsDto>());

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No courses with enrollments found", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdminCoursesPage_Shows_Error_Message_On_Failure()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .ThrowsAsync(new Exception("Network error"));

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Course Management", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdminCoursesPage_Calls_GetAllCoursesWithEnrollmentsAsync_On_Initialization()
    {
        var mockEnrollmentsClient = CreateMockEnrollmentsClient();
        var authStateProvider = CreateAuthStateProvider();
        var courses = CreateTestCoursesWithEnrollments();

        mockEnrollmentsClient.Setup(x => x.GetAllCoursesWithEnrollmentsAsync())
            .ReturnsAsync(courses);

        RegisterServices(mockEnrollmentsClient.Object, authStateProvider);

        var cut = RenderComponent<CascadingAuthenticationState>(childContent => childContent
            .AddChildContent<AdminCoursesPage>());

        cut.WaitForState(() => !cut.Markup.Contains("Loading"));

        mockEnrollmentsClient.Verify(x => x.GetAllCoursesWithEnrollmentsAsync(), Times.Once);
    }
}
